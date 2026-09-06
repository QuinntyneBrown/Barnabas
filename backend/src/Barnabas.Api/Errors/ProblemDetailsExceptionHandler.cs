using Barnabas.Application.Common.Exceptions;
using Barnabas.Domain.Congregations;
using Barnabas.Domain.Members;
using Barnabas.Domain.Access;
using Barnabas.Domain.Common;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Messaging;
using Barnabas.Domain.Requests;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Barnabas.Api.Errors;

/// <summary>
/// Turns the exceptions the domain and the pipeline raise into the one error contract.
/// </summary>
/// <remarks>
/// The mapping lives here rather than in each controller so that a status code is a property of
/// the failure, not of the endpoint that happened to hit it. Two of the choices are load-bearing
/// and deliberately counter-intuitive: a thread a caller is not party to answers 404 rather than
/// 403, and so does a resource in another congregation, because 403 would confirm the thing
/// exists. An unresolved congregation is a 500, because it means the API let an unauthenticated
/// request reach data - a bug here, not a mistake by the caller.
/// </remarks>
public sealed class ProblemDetailsExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ProblemDetailsExceptionHandler> _logger;

    public ProblemDetailsExceptionHandler(
        IProblemDetailsService problemDetails,
        ILogger<ProblemDetailsExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var problem = Describe(exception);

        if (problem is null)
        {
            return false;
        }

        _logger.LogInformation(
            "Request to {Path} refused with {Status}: {Reason}",
            httpContext.Request.Path,
            problem.Status,
            exception.GetType().Name);

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails? Describe(Exception exception) => exception switch
    {
        // Names each field at fault and never echoes what was submitted: the member already
        // knows what they typed, and echoing is how a message becomes a reflection vector.
        ValidationException validation => new ValidationProblemDetails(
            validation.Errors
                .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).ToArray(),
                    StringComparer.Ordinal))
        {
            Title = "One or more fields are invalid.",
            Status = StatusCodes.Status400BadRequest,
        },

        ForbiddenException => Problem(StatusCodes.Status403Forbidden, "The caller may not perform this action."),

        // The token was real, so this is not a 404. Saying so lets the screen offer to send
        // another link rather than implying the member mistyped something.
        SignInTokenNotRedeemableException => Problem(
            StatusCodes.Status410Gone,
            "The sign-in link has expired or has already been used."),

        RefreshTokenNotActiveException => Problem(StatusCodes.Status401Unauthorized, "The session could not be renewed."),

        SlugAlreadyTakenException => Problem(
            StatusCodes.Status409Conflict,
            "Another congregation already uses that slug."),

        EmailAlreadyRegisteredException => Problem(
            StatusCodes.Status409Conflict,
            "That email address is already in use."),

        MemberNotAwaitingApprovalException => Problem(
            StatusCodes.Status409Conflict,
            "That member is not waiting to be approved."),

        MemberNotApprovedException => Problem(
            StatusCodes.Status409Conflict,
            "That member has not been approved yet."),

        RoleNotGrantableException => Problem(
            StatusCodes.Status400BadRequest,
            "That role cannot be granted here."),

        ListingNotActiveException => Problem(StatusCodes.Status409Conflict, "The listing is no longer active."),

        ListingNotRestorableException => Problem(
            StatusCodes.Status409Conflict,
            "The listing cannot be put back on the board."),

        ListingStillOnTheBoardException => Problem(
            StatusCodes.Status409Conflict,
            "Take the listing off the board before deleting it."),

        RequestAlreadyDecidedException => Problem(StatusCodes.Status409Conflict, "The request has already been decided."),

        NotAPartyException => Problem(StatusCodes.Status404NotFound, "The thread was not found."),

        NotFoundException => Problem(StatusCodes.Status404NotFound, "The resource was not found."),

        // The four conditions a request has to satisfy do not answer alike. Asking for your own
        // listing, or through the endpoint for the wrong kind, is a malformed ask. Asking for
        // something already gone, or asking twice, is a well-formed ask the board refuses.
        RequestNotEligibleException ineligible => Problem(
            ineligible.Reason is RequestEligibility.OwnListing or RequestEligibility.KindMismatch
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status409Conflict,
            Explain(ineligible.Reason)),

        CongregationContextMissingException => Problem(
            StatusCodes.Status500InternalServerError,
            "The request could not be completed."),

        _ => null,
    };

    private static ProblemDetails Problem(int status, string title) => new() { Status = status, Title = title };

    private static string Explain(RequestEligibility reason) => reason switch
    {
        RequestEligibility.OwnListing => "You cannot request your own listing.",
        RequestEligibility.KindMismatch => "That listing does not take this kind of request.",
        RequestEligibility.ListingNotActive => "The listing is no longer active.",
        RequestEligibility.DuplicateRequest => "You already have an open request against this listing.",
        _ => "The request cannot be made against this listing.",
    };
}

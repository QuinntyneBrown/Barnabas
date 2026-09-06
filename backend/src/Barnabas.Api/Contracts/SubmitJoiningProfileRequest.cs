namespace Barnabas.Api.Contracts;

/// <summary>
/// What somebody fills in to finish joining.
/// </summary>
/// <remarks>
/// The joining token names the congregation, so there is no congregation field for a caller to
/// supply. The email address and the reason are collected for reasons ADR-0002 records: sign-in
/// needs the first, and a moderator reading the queue needs the second.
/// </remarks>
public sealed record SubmitJoiningProfileRequest(
    string JoiningToken,
    string EmailAddress,
    string DisplayName,
    string Neighbourhood,
    string? ReasonForJoining);

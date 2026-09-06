using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Congregations;
using Barnabas.Domain.Members;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Congregations.ProvisionCongregation;

/// <summary>
/// Creates the congregation and its first moderator, and commits.
/// </summary>
/// <remarks>
/// The slug is checked before the insert so the ordinary duplicate gets a clear answer, and the
/// insert is guarded so the racing duplicate gets the same one. Neither is redundant: two
/// administrators posting the same slug both pass the check before either commits, and only the
/// unique index can refuse the second.
/// <para>
/// <c>Congregations</c> is the one set the context does not gate, so this runs correctly with the
/// administrator's own congregation in scope and writes to neither.
/// </remarks>
public sealed class ProvisionCongregationCommandHandler
    : IRequestHandler<ProvisionCongregationCommand, ProvisionedCongregationResult>
{
    private readonly IBarnabasDbContext _context;
    private readonly IProvisioningStore _provisioning;

    public ProvisionCongregationCommandHandler(IBarnabasDbContext context, IProvisioningStore provisioning)
    {
        _context = context;
        _provisioning = provisioning;
    }

    public async Task<ProvisionedCongregationResult> Handle(
        ProvisionCongregationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var slug = Congregation.NormaliseSlug(request.Slug);

        if (await _provisioning.SlugIsTakenAsync(slug, cancellationToken))
        {
            throw new SlugAlreadyTakenException(slug);
        }

        if (await _provisioning.EmailIsRegisteredAsync(request.FoundingModeratorEmail, cancellationToken))
        {
            throw new EmailAlreadyRegisteredException();
        }

        var congregation = Congregation.Provision(
            Guid.NewGuid(),
            request.Name,
            slug,
            request.Neighbourhoods!);

        _context.Congregations.Add(congregation);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The index refused it, so another administrator got there first. The answer is the
            // one the check above would have given.
            throw new SlugAlreadyTakenException(slug);
        }

        await _provisioning.AddFoundingModeratorAsync(
            Guid.NewGuid(),
            congregation.Id,
            request.FoundingModeratorEmail,
            request.FoundingModeratorDisplayName,
            congregation.Neighbourhoods[0],
            cancellationToken);

        return new ProvisionedCongregationResult(congregation.Id, congregation.Name, congregation.Slug);
    }
}

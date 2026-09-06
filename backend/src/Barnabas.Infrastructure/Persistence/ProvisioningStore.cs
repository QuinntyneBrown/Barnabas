using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Infrastructure.Persistence;

/// <inheritdoc />
public sealed class ProvisioningStore : IProvisioningStore
{
    private readonly BarnabasDbContext _context;

    public ProvisioningStore(BarnabasDbContext context) => _context = context;

    public Task<bool> SlugIsTakenAsync(string slug, CancellationToken cancellationToken) =>
        _context.Congregations.AnyAsync(congregation => congregation.Slug == slug, cancellationToken);

    public Task<bool> EmailIsRegisteredAsync(string emailAddress, CancellationToken cancellationToken)
    {
        var normalised = Member.Normalise(emailAddress);

        return _context.Set<Member>()
            .IgnoreQueryFilters()
            .AnyAsync(member => member.EmailAddress == normalised, cancellationToken);
    }

    public Task<Member?> FindMemberAsync(Guid congregationId, Guid memberId, CancellationToken cancellationToken) =>
        _context.Set<Member>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                member => member.Id == memberId && member.CongregationId == congregationId,
                cancellationToken);

    public Task SaveAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);

    /// <summary>
    /// Inserts the founding moderator directly, so the congregation stamp does not claim them.
    /// </summary>
    /// <remarks>
    /// Written as a statement rather than tracked through the change tracker on purpose.
    /// <c>SaveChanges</c> stamps every added row with the caller's congregation, which is what
    /// stops a payload choosing one — and here the congregation is the one the handler has just
    /// created, not the administrator's. Going around the tracker for this single row keeps that
    /// guard intact everywhere else rather than weakening it for everyone.
    /// <para>
    /// The columns are named in full so a schema change breaks this loudly at the first
    /// acceptance test rather than quietly writing the wrong shape.
    /// </para>
    /// </remarks>
    public async Task AddFoundingModeratorAsync(
        Guid memberId,
        Guid congregationId,
        string emailAddress,
        string displayName,
        string neighbourhood,
        CancellationToken cancellationToken)
    {
        var normalised = Member.Normalise(emailAddress);

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO [Members] ([Id], [CongregationId], [EmailAddress], [DisplayName], [Neighbourhood], [Role], [Status])
             VALUES ({memberId}, {congregationId}, {normalised}, {displayName}, {neighbourhood}, {(int)MemberRole.Moderator}, {(int)MemberStatus.Approved})
             """,
            cancellationToken);
    }
}

using Barnabas.Application.Common.Persistence;
using Barnabas.Domain.Members;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Members.GetDirectory;

/// <summary>
/// Reads the congregation's approved members, optionally narrowed by name or by what they offer.
/// </summary>
/// <remarks>
/// Approved only, so somebody still waiting on a moderator and somebody who has left are both
/// absent — <c>L2-079 AC1</c>. The congregation predicate comes from the global filter rather
/// than from here.
/// <para>
/// The tag search is an exact match against the child table rather than a substring of a joined
/// string, which is why the tags are a table: "Rides" must not be found inside "Joyrides".
/// </para>
/// </remarks>
public sealed class GetDirectoryQueryHandler
    : IRequestHandler<GetDirectoryQuery, IReadOnlyList<DirectoryMemberDto>>
{
    private readonly IBarnabasDbContext _context;

    public GetDirectoryQueryHandler(IBarnabasDbContext context) => _context = context;

    public async Task<IReadOnlyList<DirectoryMemberDto>> Handle(
        GetDirectoryQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var members = _context.Members.Where(member => member.Status == MemberStatus.Approved);

        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            var term = request.Term.Trim();

            members = members.Where(member => EF.Functions.Like(member.DisplayName, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(request.HelpTag))
        {
            var tag = request.HelpTag.Trim();

            members = members.Where(member => member.HelpTags.Any(helpTag => helpTag.Tag == tag));
        }

        return await members
            .OrderBy(member => member.DisplayName)
            .ThenBy(member => member.Id)
            .Select(member => new DirectoryMemberDto(
                member.Id,
                member.DisplayName,
                member.Neighbourhood,
                member.HelpTags.Select(tag => tag.Tag).ToList()))
            .ToListAsync(cancellationToken);
    }
}

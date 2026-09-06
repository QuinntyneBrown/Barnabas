using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Members.GetMyProfile;

/// <summary>Reads the caller's own profile, and what their congregation offers.</summary>
public sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, MyProfileDto>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;

    public GetMyProfileQueryHandler(IBarnabasDbContext context, ICongregationContext congregation)
    {
        _context = context;
        _congregation = congregation;
    }

    public async Task<MyProfileDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var memberId = _congregation.MemberId;
        var congregationId = _congregation.CongregationId;

        var member = await _context.Members
            .Include(member => member.HelpTags)
            .FirstOrDefaultAsync(member => member.Id == memberId, cancellationToken)
            ?? throw new NotFoundException();

        var congregation = await _context.Congregations
            .FirstOrDefaultAsync(congregation => congregation.Id == congregationId, cancellationToken)
            ?? throw new NotFoundException();

        return new MyProfileDto(
            member.Id,
            member.DisplayName,
            member.EmailAddress,
            member.Neighbourhood,
            member.Description,
            [.. member.HelpTags.Select(tag => tag.Tag)],
            congregation.Neighbourhoods,
            congregation.HelpTags);
    }
}

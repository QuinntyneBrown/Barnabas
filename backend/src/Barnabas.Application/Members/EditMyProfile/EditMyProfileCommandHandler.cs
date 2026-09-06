using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Application.Members.GetMyProfile;
using Barnabas.Domain.Congregations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Members.EditMyProfile;

/// <summary>
/// Applies the change and commits, returning the profile as it now stands.
/// </summary>
/// <remarks>
/// The neighbourhood and the help tags are checked against the congregation here rather than in
/// the validator, because both are relationships between two aggregates and a validator holds
/// only the command. Refused as a named field either way, which is what <c>L2-022</c> asks for.
/// </remarks>
public sealed class EditMyProfileCommandHandler : IRequestHandler<EditMyProfileCommand, MyProfileDto>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;

    public EditMyProfileCommandHandler(IBarnabasDbContext context, ICongregationContext congregation)
    {
        _context = context;
        _congregation = congregation;
    }

    public async Task<MyProfileDto> Handle(EditMyProfileCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var memberId = _congregation.MemberId;
        var congregationId = _congregation.CongregationId;

        var member = await _context.Members
            .Include(member => member.HelpTags)
            .FirstOrDefaultAsync(member => member.Id == memberId, cancellationToken)
            ?? throw new NotFoundException();

        var congregation = await _context.Congregations
            .FirstOrDefaultAsync(congregation => congregation.Id == congregationId, cancellationToken)
            ?? throw new NotFoundException();

        if (!congregation.HasNeighbourhood(request.Neighbourhood))
        {
            throw new NeighbourhoodNotOfferedException(request.Neighbourhood);
        }

        var tags = request.HelpTags ?? [];

        foreach (var tag in tags)
        {
            if (!congregation.HasHelpTag(tag))
            {
                throw new HelpTagNotOfferedException(tag);
            }
        }

        member.UpdateProfile(request.DisplayName, request.Neighbourhood, request.Description, tags);

        await _context.SaveChangesAsync(cancellationToken);

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

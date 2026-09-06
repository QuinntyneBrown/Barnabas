using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Congregations.GetCongregation;

/// <summary>Reads the congregation the caller belongs to.</summary>
public sealed class GetCongregationQueryHandler : IRequestHandler<GetCongregationQuery, CongregationDto>
{
    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;

    public GetCongregationQueryHandler(IBarnabasDbContext context, ICongregationContext congregation)
    {
        _context = context;
        _congregation = congregation;
    }

    public async Task<CongregationDto> Handle(GetCongregationQuery request, CancellationToken cancellationToken)
    {
        var id = _congregation.CongregationId;

        var congregation = await _context.Congregations
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException();

        return new CongregationDto(
            congregation.Id,
            congregation.Name,
            congregation.Slug,
            congregation.Neighbourhoods);
    }
}

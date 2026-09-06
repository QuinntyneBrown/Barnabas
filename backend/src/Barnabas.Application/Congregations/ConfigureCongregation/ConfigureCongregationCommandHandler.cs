using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Congregations.ConfigureCongregation;

/// <summary>Applies the configuration and commits.</summary>
public sealed class ConfigureCongregationCommandHandler
    : IRequestHandler<ConfigureCongregationCommand, ConfiguredCongregationResult>
{
    private readonly IBarnabasDbContext _context;

    public ConfigureCongregationCommandHandler(IBarnabasDbContext context) => _context = context;

    public async Task<ConfiguredCongregationResult> Handle(
        ConfigureCongregationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var congregation = await _context.Congregations
            .FirstOrDefaultAsync(c => c.Id == request.CongregationId, cancellationToken)
            ?? throw new NotFoundException();

        congregation.Rename(request.Name);
        congregation.SetNeighbourhoods(request.Neighbourhoods!);

        await _context.SaveChangesAsync(cancellationToken);

        return new ConfiguredCongregationResult(
            congregation.Id,
            congregation.Name,
            congregation.Slug,
            congregation.Neighbourhoods);
    }
}

using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Joining.RevokeInviteCode;

/// <summary>Marks it withdrawn and commits.</summary>
public sealed class RevokeInviteCodeCommandHandler : IRequestHandler<RevokeInviteCodeCommand>
{
    private readonly IBarnabasDbContext _context;
    private readonly TimeProvider _time;

    public RevokeInviteCodeCommandHandler(IBarnabasDbContext context, TimeProvider time)
    {
        _context = context;
        _time = time;
    }

    public async Task Handle(RevokeInviteCodeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var code = await _context.InviteCodes
            .FirstOrDefaultAsync(code => code.Id == request.InviteCodeId, cancellationToken)
            ?? throw new NotFoundException();

        code.Revoke(_time.GetUtcNow());

        await _context.SaveChangesAsync(cancellationToken);
    }
}

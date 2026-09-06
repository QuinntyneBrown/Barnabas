using System.Security.Cryptography;
using Barnabas.Application.Common.Persistence;
using Barnabas.Application.Common.Tenancy;
using Barnabas.Application.Joining.Common;
using Barnabas.Domain.Congregations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Barnabas.Application.Joining.IssueInviteCode;

/// <summary>
/// Mints a code and commits it.
/// </summary>
/// <remarks>
/// A collision is astronomically unlikely at forty bits and is still handled, because the
/// alternative is returning a code that was never stored. Bounded attempts, and a failure rather
/// than a lie.
/// </remarks>
public sealed class IssueInviteCodeCommandHandler : IRequestHandler<IssueInviteCodeCommand, IssuedInviteCodeResult>
{
    private const int Attempts = 5;

    private readonly IBarnabasDbContext _context;
    private readonly ICongregationContext _congregation;
    private readonly TimeProvider _time;
    private readonly InviteCodeOptions _options;

    public IssueInviteCodeCommandHandler(
        IBarnabasDbContext context,
        ICongregationContext congregation,
        TimeProvider time,
        IOptions<InviteCodeOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _context = context;
        _congregation = congregation;
        _time = time;
        _options = options.Value;
    }

    public async Task<IssuedInviteCodeResult> Handle(
        IssueInviteCodeCommand request,
        CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        var expiresAt = now.Add(_options.Lifetime);

        for (var attempt = 0; attempt < Attempts; attempt += 1)
        {
            var code = InviteCode.Format(RandomNumberGenerator.GetBytes(InviteCode.Length));

            var invite = InviteCode.Issue(
                Guid.NewGuid(),
                _congregation.CongregationId,
                code,
                _congregation.MemberId,
                now,
                expiresAt);

            _context.InviteCodes.Add(invite);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // The unique index refused it. Try again with fresh entropy rather than handing
                // back a code that is not in the database.
                _context.InviteCodes.Remove(invite);

                continue;
            }

            return new IssuedInviteCodeResult(invite.Id, invite.Code, invite.ExpiresAt);
        }

        throw new InvalidOperationException("Could not mint a distinct invite code.");
    }
}

using Barnabas.Domain.Access;
using Barnabas.Domain.Congregations;
using Barnabas.Domain.Listings;
using Barnabas.Domain.Members;
using Barnabas.Domain.Messaging;
using Barnabas.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace Barnabas.Application.Common.Persistence;

/// <summary>
/// The data the application layer reads and writes.
/// </summary>
/// <remarks>
/// Every set here is congregation-owned and is filtered underneath, so a handler writes
/// <c>Listings.Where(l =&gt; l.Status == Active)</c> and receives only its own congregation's
/// rows. There is no <c>WithCongregation()</c> call for a handler to omit, which is the
/// point: the safe path is the only path.
/// </remarks>
public interface IBarnabasDbContext
{
    DbSet<Congregation> Congregations { get; }

    DbSet<Member> Members { get; }

    DbSet<InviteCode> InviteCodes { get; }

    DbSet<SignInToken> SignInTokens { get; }

    DbSet<Session> Sessions { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Listing> Listings { get; }

    DbSet<ListingRequest> ListingRequests { get; }

    DbSet<MessageThread> MessageThreads { get; }

    DbSet<Message> Messages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

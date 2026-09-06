namespace Barnabas.Application.Common.Persistence;

/// <summary>
/// Runs several writes as one atomic unit.
/// </summary>
/// <remarks>
/// Only two operations in feature slice 1 need this, and both need it for the same reason: a
/// half-committed result would be worse than a failure. Accepting a request writes the
/// decision and opens the thread, and a decision without a thread leaves the requester
/// notified of something they cannot act on.
/// <para>
/// It is an abstraction rather than a direct transaction call because the application layer
/// does not know which provider it is running on, and the two providers in use differ in how
/// they nest transactions.
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}

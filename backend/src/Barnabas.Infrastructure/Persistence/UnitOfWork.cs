using Barnabas.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Barnabas.Infrastructure.Persistence;

/// <inheritdoc />
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly BarnabasDbContext _context;

    public UnitOfWork(BarnabasDbContext context) => _context = context;

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // An execution strategy may retry, so the whole operation has to be replayable rather
        // than only the commit.
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            var result = await operation(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        });
    }
}

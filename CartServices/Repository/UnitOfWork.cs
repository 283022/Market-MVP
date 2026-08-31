using CartServices.Db;
using Microsoft.EntityFrameworkCore.Storage;

namespace CartServices.Repository;

public class UnitOfWork(
    AppDbContext context,
    ILogger<UnitOfWork> logger,
    CartRepository cartRepository)
    : IDisposable
{
    private IDbContextTransaction _transaction = null;
    private bool _disposed;

    public CartRepository Carts { get; } = cartRepository;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        logger.LogTrace("Saving changes to database");
        return await context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            logger.LogWarning("Transaction already exists");
            return;
        }

        logger.LogTrace("Beginning database transaction");
        _transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            logger.LogWarning("No transaction to commit");
            return;
        }

        logger.LogTrace("Committing database transaction");
        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            logger.LogWarning("No transaction to rollback");
            return;
        }

        logger.LogWarning("Rolling back database transaction");
        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _transaction?.Dispose();
        context.Dispose();
        _disposed = true;
    }
}
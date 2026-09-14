using OrderServices.Db;

namespace OrderServices.Repository;

public class UnitOfWork(ApplicationDbContext context, OrderRepository repository) : IAsyncDisposable
{
    private readonly ApplicationDbContext _context = context;

    public OrderRepository Repository { get; } = repository;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => await _context.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        => await _context.Database.CommitTransactionAsync(cancellationToken);

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        => await _context.Database.RollbackTransactionAsync(cancellationToken);

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}
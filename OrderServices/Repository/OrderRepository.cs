using Microsoft.EntityFrameworkCore;
using OrderServices.Db;
using OrderServices.Model;

namespace OrderServices.Repository;

public class OrderRepository(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<IReadOnlyList<Order>> GetOrdersByUserId(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetOrderById(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);
    }

    public async Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }
}
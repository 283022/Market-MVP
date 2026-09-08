using Microsoft.EntityFrameworkCore;
using OrderServices.Db;
using OrderServices.Model;

namespace OrderServices.Repository;

public class OrderRepository(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<List<Order>> GetOrdersByUserId(Guid userId)
    {
        return await _context.Orders.Where(x => x.UserId == userId).ToListAsync();
        
    }

    public async Task CreateOrder(Order order)
    {
        await _context.Orders.AddAsync(order);
    }

    public async Task<Order?> GetOrderById(Guid orderId)
    {
        return await _context.Orders.FirstOrDefaultAsync(x => x.Id == orderId);
    }
}
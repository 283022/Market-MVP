using CartServices.Db;
using CartServices.Models;
using Microsoft.EntityFrameworkCore;

namespace CartServices.Repository;

public class CartRepository(AppDbContext context, ILogger<CartRepository> logger)
{
    public async Task<Cart?> GetByIdAsync(Guid id)
    {
        return await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Cart?> GetByUserIdAsync(Guid userId)
    {
        return await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<Cart?> GetBySessionIdAsync(string sessionId)
    {
        return await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);
    }

    public async Task<Cart?> GetByUserIdWithItemsAsync(Guid userId)
    {
        return await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<Cart?> GetBySessionIdWithItemsAsync(string sessionId)
    {
        return await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);
    }

    public async Task AddAsync(Cart cart)
    {
        logger.LogDebug("Adding new cart: {CartId} for user {UserId}", cart.Id, cart.UserId);
        await context.Carts.AddAsync(cart);
    }

    public Task UpdateAsync(Cart cart)
    {
        logger.LogDebug("Updating cart: {CartId}", cart.Id);
        context.Carts.Update(cart);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Cart cart)
    {
        logger.LogDebug("Deleting cart: {CartId}", cart.Id);
        context.Carts.Remove(cart);
        return Task.CompletedTask;
    }
}
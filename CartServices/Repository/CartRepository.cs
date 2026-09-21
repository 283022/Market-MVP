using CartServices.Db;
using CartServices.Models;
using Microsoft.EntityFrameworkCore;

namespace CartServices.Repository;

public class CartRepository(
    AppDbContext context,
    ILogger<CartRepository> logger) : ICartRepository
{
    public async Task<Cart?> GetByIdAsync(Guid cartId)
    {
        return await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CartId == cartId);
    }

    public async Task<Cart?> GetByUserIdAsync(Guid userId)
    {
        return await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task AddAsync(Cart cart)
    {
        logger.LogDebug(
            "Adding new cart: {CartId} for user {UserId}",
            cart.CartId,
            cart.UserId);

        await context.Carts.AddAsync(cart);
    }

    public Task UpdateAsync(Cart cart)
    {
        logger.LogDebug(
            "Updating cart: {CartId}",
            cart.CartId);

        context.Carts.Update(cart);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Cart cart)
    {
        logger.LogDebug(
            "Deleting cart: {CartId}",
            cart.CartId);

        context.Carts.Remove(cart);

        return Task.CompletedTask;
    }
}
using CartServices.Models;

namespace CartServices.Repository;

public interface ICartRepository
{
    Task<Cart?> GetByIdAsync(Guid cartId);

    Task<Cart?> GetByUserIdAsync(Guid userId);

    Task AddAsync(Cart cart);

    Task UpdateAsync(Cart cart);

    Task DeleteAsync(Cart cart);
}
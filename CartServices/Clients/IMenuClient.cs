using FluentResults;

namespace CartServices.Clients;

public interface IMenuClient
{
    Task<Result<ProductDto?>> GetProductAsync(Guid productId);

    Task<Result<List<ProductDto>>> GetProductsBatchAsync(
        List<Guid> productIds);
}
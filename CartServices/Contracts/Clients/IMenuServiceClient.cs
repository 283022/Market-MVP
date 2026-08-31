using MenuServices.Contracts.DTOs;

namespace MenuServices.Contracts.Clients;

public interface IMenuServiceClient
{
    Task<ProductDto> GetProductAsync(Guid productId);
    Task<List<ProductDto>> GetProductsBatchAsync(List<Guid> productIds);
}
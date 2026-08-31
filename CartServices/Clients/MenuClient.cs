using MenuServices.Contracts.Clients;
using MenuServices.Contracts.DTOs;

namespace CartServices.Clients;

public class MenuClient : IMenuServiceClient
{
    private readonly HttpClient _client;
    private readonly ILogger<MenuClient> _logger;

    public MenuClient(HttpClient client, ILogger<MenuClient> logger)
    {
        _client = client;
        _logger = logger;
    }
    
    public async Task<ProductDto?> GetProductAsync(Guid productId)
    {
        try
        {
            var response = await _client.GetAsync($"/api/menu/{productId}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get product {ProductId}", productId);
            throw;
        }
    }

    public async Task<List<ProductDto>> GetProductsBatchAsync(List<Guid> productIds)
    {
        try
        {
            var request = new { ProductIds = productIds };
            var response = await _client.PostAsJsonAsync("/api/menu/batch", request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get products batch: {StatusCode}", response.StatusCode);
                return new List<ProductDto>();
            }
            
            return await response.Content.ReadFromJsonAsync<List<ProductDto>>() ?? new List<ProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get products batch");
            throw;
        }
    }
}
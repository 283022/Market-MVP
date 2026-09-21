using System.Net;
using FluentResults;

namespace CartServices.Clients;

public class MenuClient : IMenuClient
{
    private readonly HttpClient _client;
    private readonly ILogger<MenuClient> _logger;

    public MenuClient(HttpClient client, ILogger<MenuClient> logger)
    {
        _client = client;
        _logger = logger;
    }
    //TODO: реализовать контракты
    public async Task<Result<ProductDto?>> GetProductAsync(Guid productId)
    {
        try
        {
            var response = await _client.GetAsync($"/api/menu/{productId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
                return Result.Ok<ProductDto?>(null);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Menu service returned {StatusCode}", response.StatusCode);
                return Result.Fail<ProductDto?>(
                    new ExternalServiceError($"Menu service returned {response.StatusCode}"));
            }

            var product = await response.Content.ReadFromJsonAsync<ProductDto>();
            return Result.Ok(product);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Menu service unavailable");
            return Result.Fail<ProductDto?>(
                new ExternalServiceError($"Menu service unavailable: {ex.Message}"));
        }
    }

    public async Task<Result<List<ProductDto>>> GetProductsBatchAsync(List<Guid> productIds)
    {
        try
        {
            var request = new { ProductIds = productIds };
            var response = await _client.PostAsJsonAsync("/api/menu/batch", request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Menu batch returned {StatusCode}", response.StatusCode);
                return Result.Fail<List<ProductDto>>(
                    new ExternalServiceError($"Menu service returned {response.StatusCode}"));
            }

            var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>()
                           ?? new List<ProductDto>();

            return Result.Ok(products);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Menu service unavailable");
            return Result.Fail<List<ProductDto>>(
                new ExternalServiceError($"Menu service unavailable: {ex.Message}"));
        }
    }
}
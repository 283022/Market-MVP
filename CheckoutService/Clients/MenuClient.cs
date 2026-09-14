using System.Net.Http.Json;
using CheckoutService.DTOs;
using FluentResults;

namespace CheckoutService.Clients;

public class MenuClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MenuClient> _logger;

    public MenuClient(HttpClient httpClient, ILogger<MenuClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("http://menu-service:8080/");
    }

    public async Task<Result<ValidateProductsResponse>> ValidateProductsAsync(List<Guid> productIds)
    {
        try
        {
            var request = new { productIds };
            var response = await _httpClient.PostAsJsonAsync("/api/menu/validate", request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Menu service validate returned {StatusCode}", response.StatusCode);
                return Result.Fail<ValidateProductsResponse>(
                    new ExternalServiceError($"Menu service returned {response.StatusCode}"));
            }

            var payload = await response.Content.ReadFromJsonAsync<ValidateProductsResponse>();
            if (payload is null)
                return Result.Fail<ValidateProductsResponse>(
                    new ExternalServiceError("Menu service returned empty validate response"));

            return Result.Ok(payload);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Menu service unavailable");
            return Result.Fail<ValidateProductsResponse>(
                new ExternalServiceError($"Menu service unavailable: {ex.Message}"));
        }
    }

    public async Task<Result<ProductDetailsResponse>> GetProductDetailsAsync(List<Guid> productIds)
    {
        try
        {
            var request = new { productIds };
            var response = await _httpClient.PostAsJsonAsync("/api/menu/details", request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Menu service details returned {StatusCode}", response.StatusCode);
                return Result.Fail<ProductDetailsResponse>(
                    new ExternalServiceError($"Menu service returned {response.StatusCode}"));
            }

            var payload = await response.Content.ReadFromJsonAsync<ProductDetailsResponse>();
            if (payload is null)
                return Result.Fail<ProductDetailsResponse>(
                    new ExternalServiceError("Menu service returned empty details response"));

            return Result.Ok(payload);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Menu service unavailable");
            return Result.Fail<ProductDetailsResponse>(
                new ExternalServiceError($"Menu service unavailable: {ex.Message}"));
        }
    }
}
using System.Net.Http.Json;
using CheckoutService.DTOs;
using FluentResults;

namespace CheckoutService.Clients;

public class CartClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CartClient> _logger;

    public CartClient(HttpClient httpClient, ILogger<CartClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("http://cart-service:8080/");
    }

    public async Task<Result<CartResponse>> GetCartAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/cart/my?userId={userId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Cart service returned {StatusCode}", response.StatusCode);
                return Result.Fail<CartResponse>(
                    new ExternalServiceError($"Cart service returned {response.StatusCode}"));
            }

            var cart = await response.Content.ReadFromJsonAsync<CartResponse>();
            if (cart is null)
                return Result.Fail<CartResponse>(
                    new ExternalServiceError("Cart service returned empty response"));

            return Result.Ok(cart);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Cart service unavailable");
            return Result.Fail<CartResponse>(
                new ExternalServiceError($"Cart service unavailable: {ex.Message}"));
        }
    }
}
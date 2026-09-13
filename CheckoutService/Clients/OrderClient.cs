using System.Net.Http.Json;
using CheckoutService.DTOs;
using FluentResults;

namespace CheckoutService.Clients;

public class OrderClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderClient> _logger;

    public OrderClient(HttpClient httpClient, ILogger<OrderClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("http://order-service:8080/");
    }

    public async Task<Result<OrderResponse>> CreateOrderAsync(
        Guid userId, CreateOrderRequestDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/orders/create?userId={userId}", request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Order service returned {StatusCode}: {Body}",
                    response.StatusCode, body);

                return Result.Fail<OrderResponse>(
                    new ExternalServiceError($"Order service returned {response.StatusCode}: {body}"));
            }

            var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
            if (order is null)
                return Result.Fail<OrderResponse>(
                    new ExternalServiceError("Order service returned empty response"));

            return Result.Ok(order);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Order service unavailable");
            return Result.Fail<OrderResponse>(
                new ExternalServiceError($"Order service unavailable: {ex.Message}"));
        }
    }
}
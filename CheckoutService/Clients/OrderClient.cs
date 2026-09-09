using CheckoutService.DTOs;

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

    public async Task<OrderResponse> CreateOrderAsync(Guid userId, CreateOrderRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/orders/create?userId={userId}", request);
        response.EnsureSuccessStatusCode();
        a
        return await response.Content.ReadFromJsonAsync<OrderResponse>();
    }
}
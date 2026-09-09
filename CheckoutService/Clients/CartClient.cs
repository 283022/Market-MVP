using CheckoutService.DTOs;
namespace CheckoutService.Clients;

public class CartClient : HttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CartClient> _logger;

    public CartClient(HttpClient httpClient, ILogger<CartClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("http://cart-service:8080/"); 
    }

    public async Task<CartResponse> GetCartAsync(Guid userId)
    {
        var response = await _httpClient.GetAsync($"/api/cart/my?userId={userId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CartResponse>();
    }
}
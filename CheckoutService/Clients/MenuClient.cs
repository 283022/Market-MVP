using CheckoutService.DTOs;

namespace CheckoutService.Clients;

public class MenuClient : HttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MenuClient> _logger;

    public MenuClient(HttpClient httpClient, ILogger<MenuClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("http://menu-service:8080/");
    }

    public async Task<ValidateProductsResponse> ValidateProductsAsync(List<Guid> productIds)
    {
        var request = new { productIds };
        var response = await _httpClient.PostAsJsonAsync("/api/menu/validate", request);
        response.EnsureSuccessStatusCode();
        a
        return await response.Content.ReadFromJsonAsync<ValidateProductsResponse>();
    }

    public async Task<ProductDetailsResponse> GetProductDetailsAsync(List<Guid> productIds)
    {
        var request = new { productIds };
        var response = await _httpClient.PostAsJsonAsync("/api/menu/details", request);
        response.EnsureSuccessStatusCode();
        a
        return await response.Content.ReadFromJsonAsync<ProductDetailsResponse>();
    }
}
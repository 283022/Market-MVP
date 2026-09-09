namespace CheckoutService.DTOs;

public class ValidateProductsRequest
{
    public List<Guid> ProductIds { get; set; }
}

public class ValidateProductsResponse
{
    public bool IsValid { get; set; }
    public List<ProductValidationError> Errors { get; set; } = new();
}

public class ProductValidationError
{
    public Guid ProductId { get; set; }
    public string Reason { get; set; }
}

public class ProductDetailsResponse
{
    public List<ProductDetailDto> Products { get; set; } = new();
}

public class ProductDetailDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public bool IsStopped { get; set; }
}
namespace CheckoutService.DTOs;

public class CreateOrderRequestDto
{
    public List<OrderItemDto> Items { get; set; }
    public string? Comment { get; set; }
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class OrderResponse
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string PaymentUrl { get; set; }
}
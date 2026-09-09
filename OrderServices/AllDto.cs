namespace OrderServices;

public class CreateOrderRequest
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

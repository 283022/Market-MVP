namespace CartServices;


public record AddCartItemRequest(
    Guid ProductId, 
    int Quantity);

public record UpdateCartItemRequest(int Quantity);

public record RemoveItemsRequest(List<Guid> ItemIds);

public record CartDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public List<CartItemDto> Items { get; init; } = new();
    public decimal TotalPrice { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CartItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public string ImageUrl { get; init; }
    public bool IsStopped { get; init; }
    public string Category { get; init; }
}

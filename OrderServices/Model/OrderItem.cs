namespace OrderServices.Model;

public class OrderItem
{
    //id created by ef core
    public Guid Id {get; private set;}
    public Guid ProductId {get; private set;}
    public int Quantity {get; private set;}
    public decimal UnitPrice {get; private set;}
    public string ProductName { get; private set; }

    private OrderItem(Guid productId, int quantity, decimal unitPrice, string productName)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        ProductName = productName;
    }
    
    public static OrderItem Create(Guid productId, int quantity, decimal unitPrice, string productName)
    {
        return new OrderItem(productId, quantity, unitPrice, productName);
    }
}
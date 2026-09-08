namespace OrderServices.Model;

public class OrderItem
{
    public Guid Id {get; private set;}
    public Guid ProductId {get; private set;}
    public int Quantity {get; private set;}
    public decimal UnitPrice {get; private set;}
    public string ProductName { get; private set; }
}
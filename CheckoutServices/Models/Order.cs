namespace CheckoutService.Models;

public class Order
{
    public List<Item> Items { get;private set; }
    public DateTime OrderDate { get;private set; }

    private Order(List<Item> items)
    {
        Items = items;
        OrderDate = DateTime.UtcNow;
    }
    
    public static Order Create(List<Item> items)
    {
        return new Order(items);
    }
}
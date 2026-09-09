namespace CheckoutServices.Models;

public class Item
{
    public Guid ProductId { get; private set ; }
    public int Quantity { get; private set; }
    public decimal Price { get; private set; }
    public decimal TotalPrice { get; private set; }

    private Item(Guid productId, 
        int quantity, decimal price)
    {
        ProductId = productId;
        Quantity = quantity;
        Price = price;
        TotalPrice = price * quantity;
    }

    public static Item Create(Guid productId,
        int quantity, decimal price)
    {

        return new Item(productId, quantity, price);
    }
}
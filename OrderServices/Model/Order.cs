namespace OrderServices.Model;

public class Order
{
    public Guid Id { get; private set; }
    public Guid UserId { get; set; }
    public DateTime OrderDate { get; private set; }
    public DateTime OrderUpdate { get; private set; }
    public OrderStatus OrderStatus { get; set; }
    
    public DateTime? TimeToEndPending {get; private set;}

    public List<OrderItem> Items { get; private set; }
    
    private Order(Guid userId)
    {
        OrderDate = DateTime.UtcNow;
        OrderUpdate = DateTime.UtcNow;
        OrderStatus = OrderStatus.Pending;
        UserId = userId;
    }
    
    public static Order Create(Guid userId)
    {
        return new Order(userId);
    }

    public void DateUpdate()
    {
        OrderUpdate = DateTime.UtcNow;
    }
    
    public void ConfirmPayment()
    {
        TimeToEndPending = null;
        OrderStatus = OrderStatus.Processing;
        OrderUpdate = DateTime.UtcNow;
    }
}
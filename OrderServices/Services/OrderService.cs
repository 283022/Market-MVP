using OrderServices.Model;
using OrderServices.Repository;

namespace OrderServices.Services;

public class OrderService(UnitOfWork unitOfWork)
{
    private readonly UnitOfWork _unitOfWork = unitOfWork;

    public async Task<IEnumerable<Order>> GetUserOrdersById(Guid userId)
    {
        return await _unitOfWork.Repository.GetOrdersByUserId(userId);
    }

    public async Task<Order> GetOrderById(Guid orderId, Guid userId)
    {
        var order = await _unitOfWork.Repository.GetOrderById(orderId);
        if (order is null)
            throw new Exception("Cannot find order");
        if (order.UserId != userId)
            throw new Exception("Authorization Denied");
        return order;
    }
    
    public async Task<Order> CreateOrderAsync(Guid userId, CreateOrderRequest request)
    {
        // 1. Создаем сущности OrderItem
        var items = request.Items.Select(i =>  OrderItem.Create(
            productId: i.ProductId,
            productName: i.ProductName,
            quantity: i.Quantity,
            unitPrice: i.UnitPrice
        )).ToList();

        // 2. Создаем заказ
        var order = Order.Create(
            userId: userId,
            items: items,
            comment: request.Comment
        );

        // 3. Сохраняем в БД
        await _unitOfWork.Repository.CreateOrder(order);
        await _unitOfWork.SaveChangesAsync();

        return order;
    }

    public async Task<OrderStatus> GetStatus(Guid orderId, Guid userid)
    {
        var order = await _unitOfWork.Repository.GetOrderById(orderId);
        
        if (order is null)
            throw new Exception("Cannot find order");
        if (order.UserId != userid)
            throw new Exception("Authorization Denied");

        return order.OrderStatus;
    }

    public async Task CancelOrder(Guid orderId, Guid userid)
    {
        var order = await _unitOfWork.Repository.GetOrderById(orderId);
        
        if(order is null)
            throw new Exception("Cannot find order");
        if(order.UserId != userid)
            throw new Exception("Authorization Denied");
        
        order.OrderStatus = OrderStatus.Cancelled;
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task PaymentConfirm(Guid orderId)
    {
        var order = await _unitOfWork.Repository.GetOrderById(orderId);
        if (order is null)
            throw new Exception("Cannot find order");
        order.ConfirmPayment();
        await _unitOfWork.SaveChangesAsync();
        
    }
}


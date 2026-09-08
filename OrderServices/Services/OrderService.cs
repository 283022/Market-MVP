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
    
    public async Task CreateOrder(Guid userid)
    {
        var order = Order.Create(userid);
        if(order is null)
            throw new Exception("Cannot create order");
        await _unitOfWork.Repository.CreateOrder(order);
        await _unitOfWork.SaveChangesAsync();
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
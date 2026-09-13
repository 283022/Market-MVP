using FluentResults;
using OrderServices.Model;
using OrderServices.Repository;

namespace OrderServices.Services;

public class OrderService(UnitOfWork unitOfWork)
{
    private readonly UnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<IReadOnlyList<Order>>> GetUserOrdersById(Guid userId)
    {
        var orders = await _unitOfWork.Repository.GetOrdersByUserId(userId);
        return Result.Ok(orders);
    }

    public async Task<Result<Order>> GetOrderById(Guid orderId, Guid userId)
    {
        var order = await _unitOfWork.Repository.GetOrderById(orderId);
        if (order is null)
            return Result.Fail<Order>(new NotFoundError("Cannot find order"));
        if (order.UserId != userId)
            return Result.Fail<Order>(new ForbiddenError("Authorization Denied"));

        return Result.Ok(order);
    }

    public async Task<Result<Order>> CreateOrderAsync(Guid userId, CreateOrderRequest request)
    {
        if (request.Items is null || !request.Items.Any())
            return Result.Fail<Order>(new ValidationError("Order must contain at least one item"));

        var items = request.Items.Select(i => OrderItem.Create(
            productId: i.ProductId,
            productName: i.ProductName,
            quantity: i.Quantity,
            unitPrice: i.UnitPrice
        )).ToList();

        var order = Order.Create(userId, items, request.Comment);

        await _unitOfWork.Repository.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return Result.Ok(order);
    }

    public async Task<Result<OrderStatus>> GetStatus(Guid orderId, Guid userId)
    {
        var order = await _unitOfWork.Repository.GetOrderById(orderId);
        if (order is null)
            return Result.Fail<OrderStatus>(new NotFoundError("Cannot find order"));
        if (order.UserId != userId)
            return Result.Fail<OrderStatus>(new ForbiddenError("Authorization Denied"));

        return Result.Ok(order.OrderStatus);
    }

    public async Task<Result> CancelOrder(Guid orderId, Guid userId)
    {
        var order = await _unitOfWork.Repository.GetOrderById(orderId);
        if (order is null)
            return Result.Fail(new NotFoundError("Cannot find order"));
        if (order.UserId != userId)
            return Result.Fail(new ForbiddenError("Authorization Denied"));
        if (order.OrderStatus == OrderStatus.Cancelled)
            return Result.Fail(new ValidationError("Order already cancelled"));

        order.OrderStatus = OrderStatus.Cancelled;
        await _unitOfWork.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> PaymentConfirm(Guid orderId)
    {
        var order = await _unitOfWork.Repository.GetOrderById(orderId);
        if (order is null)
            return Result.Fail(new NotFoundError("Cannot find order"));

        order.ConfirmPayment();
        await _unitOfWork.SaveChangesAsync();
        return Result.Ok();
    }
}
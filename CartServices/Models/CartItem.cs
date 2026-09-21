using FluentResults;

namespace CartServices.Models;

public class CartItem
{
    public Guid Id { get; private set; }
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    // Navigation
    public Cart Cart { get; private set; }

    //  Приватный конструктор для EF Core
    private CartItem() { }

    //  Фабричный метод
    public static Result<CartItem> Create(Guid cartId, Guid productId, int quantity)
    {
        if (cartId == Guid.Empty)
            return Result.Fail(new ValidationError("cartId cannot be empty"));
        if (productId == Guid.Empty)
            return Result.Fail(new ValidationError("productId cannot be empty"));
        
        if (quantity <= 0)
            return Result.Fail(new ValidationError("quantity must be greater than zero"));
        return new CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cartId,
            ProductId = productId,
            Quantity = quantity
        };
    }

    //  Метод для обновления количества
    public Result UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            return Result.Fail(new ValidationError("Quantity must be greater than zero"));
        
        Quantity = newQuantity;
        return  Result.Ok();
    }

    public Result AddQuantity(int quantityToAdd)
    {
        if (quantityToAdd <= 0)
            return Result.Fail(new ValidationError("Quantity must be greater than zero"));
        Quantity += quantityToAdd;
        return Result.Ok();
    }
}
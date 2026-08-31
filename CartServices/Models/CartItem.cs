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
    public static CartItem Create(Guid cartId, Guid productId, int quantity)
    {
        if (cartId == Guid.Empty)
            throw new ArgumentException("CartId cannot be empty", nameof(cartId));
        
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty", nameof(productId));
        
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        return new CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cartId,
            ProductId = productId,
            Quantity = quantity
        };
    }

    //  Метод для обновления количества
    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(newQuantity));
        
        Quantity = newQuantity;
    }

    public void AddQuantity(int quantityToAdd)
    {
        if (quantityToAdd <= 0)
            throw new ArgumentException("Quantity to add must be greater than zero", nameof(quantityToAdd));
        
        Quantity += quantityToAdd;
    }
}
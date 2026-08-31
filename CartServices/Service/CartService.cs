using CartServices.Clients;
using CartServices.Models;
using CartServices.Repository;

namespace CartServices.Service;

public class CartService
{
    private readonly UnitOfWork _unitOfWork;
    private readonly ILogger<CartService> _logger;
    private readonly MenuClient _menuClient;

    public CartService(
        UnitOfWork unitOfWork,
        ILogger<CartService> logger, MenuClient menuClient)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _menuClient = menuClient;
    }

    //Get
    public async Task<CartDto> GetCartAsync(Guid? userId, string? sessionId)
    {
        Cart? cart = null;
        if (userId.HasValue)
        {
            cart = await _unitOfWork.Carts.GetByUserIdWithItemsAsync(userId.Value);
        }
        else if (!string.IsNullOrEmpty(sessionId))
        {
            cart = await _unitOfWork.Carts.GetBySessionIdWithItemsAsync(sessionId);
        }

        if (cart == null)
            throw new Exception();
        var result = MapToDto(cart);

        return result ?? throw new Exception();
    }

    public async Task<int> GetCartCountAsync(Guid? userId, string? sessionId)
    {
        var cart = await GetCartEntityAsync(userId, sessionId);
        return cart?.Items?.Sum(i => i.Quantity) ?? 0;
    }


    public async Task<CartDto> AddItemAsync(Guid? userId, string? sessionId, AddCartItemRequest request)
    {
        // 1. Получаем данные товара из Menu Service
        var product = await _menuClient.GetProductAsync(request.ProductId);
        if (product == null)
            throw new Exception($"Product {request.ProductId} not found");

        if (product.IsStopped)
            throw new Exception($"Product  is stopped");

        // 2. Получаем или создаем корзину
        var cart = await GetOrCreateCartAsync(userId, sessionId);

        // 3. Добавляем товар
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (existingItem != null)
        {
            existingItem.UpdateQuantity(request.Quantity);
        }
        else
        {
            cart.Items.Add(
                CartItem.Create(cart.Id, request.ProductId, request.Quantity)
            );
        }

        cart.UpdateTimestamp();
        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(cart);
    }

    // UPDATE 

    public async Task UpdateItemQuantityAsync(Guid? userId, string? sessionId, Guid itemId, int quantity)
    {
        var cart = await GetCartEntityAsync(userId, sessionId);
        if (cart == null)
            throw new Exception("Cart not found");

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            throw new Exception($"Item {itemId} not found in cart");

        // количество должно быть >= 0
        if (quantity < 0)
            throw new Exception("Quantity cannot be negative");

        // Если количество = 0 — удаляем товар
        if (quantity == 0)
        {
            cart.Items.Remove(item);
            _logger.LogDebug("Removed item: {ItemId}", itemId);
        }
        else
        {
            item.UpdateQuantity(quantity);
            cart.UpdateTimestamp();
            _logger.LogDebug("Updated item quantity: {ItemId} x {Quantity}", itemId, quantity);
        }

        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();
    }

    //delete

    public async Task RemoveItemAsync(Guid? userId, string? sessionId, Guid itemId)
    {
        var cart = await GetCartEntityAsync(userId, sessionId);
        if (cart == null)
            throw new Exception("Cart not found");

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            throw new Exception($"Item {itemId} not found in cart");

        cart.Items.Remove(item);
        cart.UpdateTimestamp();

        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RemoveItemsAsync(Guid? userId, string? sessionId, List<Guid> itemIds)
    {
        var cart = await GetCartEntityAsync(userId, sessionId);
        if (cart == null)
            throw new Exception("Cart not found");

        cart.Items.RemoveAll(i => itemIds.Contains(i.Id));
        cart.UpdateTimestamp();

        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ClearCartAsync(Guid? userId, string? sessionId)
    {
        var cart = await GetCartEntityAsync(userId, sessionId);
        if (cart == null) return;

        cart.Items.Clear();
        cart.UpdateTimestamp();

        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();
    }


    public async Task MergeCartsAsync(Guid userId, string? sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            throw new Exception("У пользователя нет корзины");

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            // 1. Получаем корзины
            var userCart = await _unitOfWork.Carts.GetByUserIdWithItemsAsync(userId);
            var sessionCart = await _unitOfWork.Carts.GetBySessionIdWithItemsAsync(sessionId);

            // 2. Если анонимной корзины нет — просто возвращаем пользовательскую
            if (sessionCart == null || !sessionCart.Items.Any())
            {
                await _unitOfWork.Carts.DeleteAsync(sessionCart);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return;
            }

            // 3. Если у пользователя нет корзины — переносим анонимную
            if (userCart == null)
            {
                sessionCart.AssignToUser(userId);
                await _unitOfWork.Carts.UpdateAsync(sessionCart);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return;
            }

            // 4. Объединяем корзины
            foreach (var sessionItem in sessionCart.Items)
            {
                var existingItem = userCart.Items
                    .FirstOrDefault(i => i.ProductId == sessionItem.ProductId);

                if (existingItem != null)
                {
                    existingItem.AddQuantity(sessionItem.Quantity);
                }
                else
                {
                    userCart.Items.Add(CartItem.Create(
                        userCart.Id,
                        sessionItem.ProductId,
                        sessionItem.Quantity));
                }
            }

            // 5. Удаляем анонимную корзину
            await _unitOfWork.Carts.DeleteAsync(sessionCart);

            userCart.UpdateTimestamp();
            await _unitOfWork.Carts.UpdateAsync(userCart);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Merged session cart {SessionId} into user cart {UserId}", sessionId, userId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error merging carts");
            throw;
        }
    }

    //HELPERS

    private async Task<Cart> GetOrCreateCartAsync(Guid? userId, string? sessionId)
    {
        var cart = await GetCartEntityAsync(userId, sessionId);
        if (cart != null) return cart;

        cart = Cart.Create(
            userId,
            sessionId);
        await _unitOfWork.Carts.AddAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        return cart;
    }

    private async Task<Cart?> GetCartEntityAsync(Guid? userId, string? sessionId)
    {
        if (userId.HasValue)
            return await _unitOfWork.Carts.GetByUserIdWithItemsAsync(userId.Value);

        if (!string.IsNullOrEmpty(sessionId))
            return await _unitOfWork.Carts.GetBySessionIdWithItemsAsync(sessionId);

        return null;
    }

    private static CartDto MapToDto(Cart cart)
    {
        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Items = cart.Items.Select(i => new CartItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                Quantity = i.Quantity,
            }).ToList(),
            UpdatedAt = cart.UpdatedAt
        };
    }
}
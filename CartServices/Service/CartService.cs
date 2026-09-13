using CartServices.Clients;
using CartServices.Models;
using CartServices.Repository;
using FluentResults;

namespace CartServices.Service;

public class CartService
{
    private readonly UnitOfWork _unitOfWork;
    private readonly ILogger<CartService> _logger;
    private readonly MenuClient _menuClient;

    public CartService(UnitOfWork unitOfWork, ILogger<CartService> logger, MenuClient menuClient)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _menuClient = menuClient;
    }

    public async Task<Result<CartDto>> GetCartAsync(Guid? userId, string? sessionId)
    {
        var cart = await GetCartEntityAsync(userId, sessionId);
        //TODO: refactoring this;
        
        // Нет корзины = пустая корзина, а не ошибка
        if (cart is null)
            return Result.Ok(new CartDto { Items = new List<CartItemDto>() });

        return Result.Ok(MapToDto(cart));
    }

    public async Task<Result<int>> GetCartCountAsync(Guid? userId, string? sessionId)
    {
        var cart = await GetCartEntityAsync(userId, sessionId);
        return Result.Ok(cart?.Items?.Sum(i => i.Quantity) ?? 0);
    }

    public async Task<Result<CartDto>> AddItemAsync(
        Guid? userId, string? sessionId, AddCartItemRequest request)
    {
        if (request.Quantity <= 0)
            return Result.Fail<CartDto>(
                new ValidationError("Quantity must be greater than zero"));

        // 1. Получаем данные товара из Menu Service
        var productResult = await _menuClient.GetProductAsync(request.ProductId);
        if (productResult.IsFailed)
            return Result.Fail<CartDto>(productResult.Errors);

        var product = productResult.Value;
        if (product is null)
            return Result.Fail<CartDto>(
                new NotFoundError($"Product {request.ProductId} not found"));

        if (product.IsStopped)
            return Result.Fail<CartDto>(
                new CartStateError($"Product {request.ProductId} is stopped"));

        // 2. Получаем или создаем корзину
        var cartResult = await GetOrCreateCartAsync(userId, sessionId);
        if (cartResult.IsFailed)
            return Result.Fail<CartDto>(cartResult.Errors);

        var cart = cartResult.Value;

        // 3. Добавляем товар
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (existingItem is not null)
        {
            existingItem.AddQuantity(request.Quantity);
        }
        else
        {
            cart.Items.Add(CartItem.Create(cart.Id, request.ProductId, request.Quantity));
        }

        cart.UpdateTimestamp();
        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        return Result.Ok(MapToDto(cart));
    }

    public async Task<Result> UpdateItemQuantityAsync(
        Guid? userId, string? sessionId, Guid itemId, int quantity)
    {
        if (quantity < 0)
            return Result.Fail(new ValidationError("Quantity cannot be negative"));

        var cart = await GetCartEntityAsync(userId, sessionId);
        if (cart is null)
            return Result.Fail(new NotFoundError("Cart not found"));

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return Result.Fail(new NotFoundError($"Item {itemId} not found in cart"));

        if (quantity == 0)
        {
            cart.Items.Remove(item);
            _logger.LogDebug("Removed item: {ItemId}", itemId);
        }
        else
        {
            item.UpdateQuantity(quantity);
            _logger.LogDebug("Updated item quantity: {ItemId} x {Quantity}", itemId, quantity);
        }

        cart.UpdateTimestamp();
        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        return Result.Ok();
    }

    public async Task<Result> RemoveItemAsync(Guid? userId, string? sessionId, Guid itemId)
    {
        var cart = await GetCartEntityAsync(userId, sessionId);
        if (cart is null)
            return Result.Fail(new NotFoundError("Cart not found"));

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return Result.Fail(new NotFoundError($"Item {itemId} not found in cart"));

        cart.Items.Remove(item);
        cart.UpdateTimestamp();

        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        return Result.Ok();
    }

    public async Task<Result> RemoveItemsAsync(
        Guid? userId, string? sessionId, List<Guid> itemIds)
    {
        if (itemIds is null || itemIds.Count == 0)
            return Result.Fail(new ValidationError("itemIds must not be empty"));

        var cart = await GetCartEntityAsync(userId, sessionId);
        if (cart is null)
            return Result.Fail(new NotFoundError("Cart not found"));

        // Проверяем, что все переданные id действительно есть в корзине,
        // и собираем ВСЕ отсутствующие позиции.
        var existingIds = cart.Items.Select(i => i.Id).ToHashSet();
        var missing = itemIds.Where(id => !existingIds.Contains(id)).ToList();

        if (missing.Any())
        {
            return Result.Fail(missing.Select(id =>
                (IError)new NotFoundError($"Item {id} not found in cart")).ToList());
        }

        cart.Items.RemoveAll(i => itemIds.Contains(i.Id));
        cart.UpdateTimestamp();

        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        return Result.Ok();
    }

    public async Task<Result> ClearCartAsync(Guid? userId, string? sessionId)
    {
        var cart = await GetCartEntityAsync(userId, sessionId);
        if (cart is null)
            return Result.Ok(); // идемпотентно

        cart.Items.Clear();
        cart.UpdateTimestamp();

        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        return Result.Ok();
    }

    public async Task<Result> MergeCartsAsync(Guid userId, string? sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return Result.Fail(new ValidationError("У пользователя нет корзины"));

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var userCart = await _unitOfWork.Carts.GetByUserIdWithItemsAsync(userId);
            var sessionCart = await _unitOfWork.Carts.GetBySessionIdWithItemsAsync(sessionId);

            // 1. Если анонимной корзины нет — просто коммитим и выходим.
            if (sessionCart is null || !sessionCart.Items.Any())
            {
                if (sessionCart is not null)
                    await _unitOfWork.Carts.DeleteAsync(sessionCart);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return Result.Ok();
            }

            // 2. Если у пользователя нет корзины — переносим анонимную.
            if (userCart is null)
            {
                sessionCart.AssignToUser(userId);
                await _unitOfWork.Carts.UpdateAsync(sessionCart);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return Result.Ok();
            }

            // 3. Объединяем корзины.
            foreach (var sessionItem in sessionCart.Items)
            {
                var existingItem = userCart.Items
                    .FirstOrDefault(i => i.ProductId == sessionItem.ProductId);

                if (existingItem is not null)
                    existingItem.AddQuantity(sessionItem.Quantity);
                else
                    userCart.Items.Add(CartItem.Create(
                        userCart.Id, sessionItem.ProductId, sessionItem.Quantity));
            }

            await _unitOfWork.Carts.DeleteAsync(sessionCart);
            userCart.UpdateTimestamp();
            await _unitOfWork.Carts.UpdateAsync(userCart);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation(
                "Merged session cart {SessionId} into user cart {UserId}",
                sessionId, userId);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            // Инфраструктурный сбой: откатываем транзакцию и логируем.
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error merging carts");

            return Result.Fail(new ExternalServiceError(
                $"Не удалось объединить корзины: {ex.Message}"));
        }
    }

    // ---------- HELPERS ----------

    private async Task<Result<Cart>> GetOrCreateCartAsync(Guid? userId, string? sessionId)
    {
        var cart = await GetCartEntityAsync(userId, sessionId);
        if (cart is not null)
            return Result.Ok(cart);

        if (!userId.HasValue && string.IsNullOrEmpty(sessionId))
            return Result.Fail<Cart>(
                new ValidationError("Either UserId or SessionId must be provided"));

        cart = Cart.Create(userId, sessionId);
        await _unitOfWork.Carts.AddAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        return Result.Ok(cart);
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
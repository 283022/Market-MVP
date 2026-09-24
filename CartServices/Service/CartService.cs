using CartServices.Clients;
using CartServices.Models;
using CartServices.Repository;
using FluentResults;

namespace CartServices.Service;

public class CartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CartService> _logger;
    private readonly IMenuClient _menuClient;

    public CartService(
        IUnitOfWork unitOfWork,
        ILogger<CartService> logger,
        IMenuClient menuClient)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _menuClient = menuClient;
    }

    public async Task<Result<CartDto>> CreateAsync(Guid? userId,Guid? cartId)
    {
        // У авторизованного пользователя может быть только одна корзина.
        if (userId.HasValue)
        {
            var existingCart = await _unitOfWork.Carts
                .GetByUserIdAsync(userId.Value);

            if (existingCart is not null)
            {
                return Result.Fail<CartDto>(
                    new ConflictError("User already has a cart"));
            }
        }
        if (cartId.HasValue)
            return Result.Fail<CartDto>(
            new ConflictError("Useralready has a cart"));

        var cart = Cart.Create(userId);

        await _unitOfWork.Carts.AddAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Created cart {CartId} for user {UserId}",
            cart.CartId,
            cart.UserId);

        return Result.Ok(MapToDto(cart));
    }
    
    public async Task<Result<CartDto>> GetCartAsync(
        Guid? userId,
        Guid? cartId)
    {
        var cart = await GetCartEntityAsync(
            userId,
            cartId);

        if (cart is null)
        {
            return Result.Ok(new CartDto
            {
                Items = new List<CartItemDto>()
            });
        }

        return Result.Ok(MapToDto(cart));
    }

    public async Task<Result<int>> GetCartCountAsync(
        Guid? userId,
        Guid? cartId)
    {
        var cart = await GetCartEntityAsync(
            userId,
            cartId);

        return Result.Ok(
            cart?.Items.Sum(i => i.Quantity) ?? 0);
    }

    public async Task<Result<CartDto>> AddItemAsync(
        Guid? userId,
        Guid? cartId,
        AddCartItemRequest request)
    {
        if (request.Quantity <= 0)
        {
            return Result.Fail<CartDto>(
                new ValidationError(
                    "Quantity must be greater than zero"));
        }

        // Проверяем товар через Menu Service.
        var productResult =
            await _menuClient.GetProductAsync(
                request.ProductId);

        if (productResult.IsFailed)
        {
            return Result.Fail<CartDto>(
                productResult.Errors);
        }

        var product = productResult.Value;

        if (product is null)
        {
            return Result.Fail<CartDto>(
                new NotFoundError(
                    $"Product {request.ProductId} not found"));
        }

        if (product.IsStopped)
        {
            return Result.Fail<CartDto>(
                new CartStateError(
                    $"Product {request.ProductId} is stopped"));
        }

        // Получаем или создаём корзину.
        var cartResult =
            await GetOrCreateCartAsync(
                userId,
                cartId);

        if (cartResult.IsFailed)
        {
            return Result.Fail<CartDto>(
                cartResult.Errors);
        }

        var cart = cartResult.Value;

        // Создаём CartItem.
        var itemResult = CartItem.Create(
            cart.CartId,
            request.ProductId,
            request.Quantity);

        if (itemResult.IsFailed)
        {
            return Result.Fail<CartDto>(
                itemResult.Errors);
        }

        // Cart сам решает:
        // добавить новый item или увеличить существующий.
        var addResult = cart.AddItem(
            itemResult.Value);

        if (addResult.IsFailed)
        {
            return Result.Fail<CartDto>(
                addResult.Errors);
        }

        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        return Result.Ok(MapToDto(cart));
    }

    public async Task<Result> UpdateItemQuantityAsync(
        Guid? userId,
        Guid? cartId,
        Guid itemId,
        int quantity)
    {
        if (quantity < 0)
        {
            return Result.Fail(
                new ValidationError(
                    "Quantity cannot be negative"));
        }

        var cart = await GetCartEntityAsync(
            userId,
            cartId);

        if (cart is null)
        {
            return Result.Fail(
                new NotFoundError(
                    "Cart not found"));
        }

        var result = cart.UpdateItemQuantity(
            itemId,
            quantity);

        if (result.IsFailed)
        {
            return result;
        }

        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogDebug(
            "Updated item {ItemId} in cart {CartId}",
            itemId,
            cart.CartId);

        return Result.Ok();
    }

    public async Task<Result> RemoveItemAsync(
        Guid? userId,
        Guid? cartId,
        Guid itemId)
    {
        var cart = await GetCartEntityAsync(
            userId,
            cartId);

        if (cart is null)
        {
            return Result.Fail(
                new NotFoundError(
                    "Cart not found"));
        }

        var result = cart.RemoveItem(itemId);

        if (result.IsFailed)
        {
            return result;
        }

        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogDebug(
            "Removed item {ItemId} from cart {CartId}",
            itemId,
            cart.CartId);

        return Result.Ok();
    }

    public async Task<Result> RemoveItemsAsync(
        Guid? userId,
        Guid? cartId,
        List<Guid> itemIds)
    {
        if (itemIds is null || itemIds.Count == 0)
        {
            return Result.Fail(
                new ValidationError(
                    "itemIds must not be empty"));
        }

        var cart = await GetCartEntityAsync(
            userId,
            cartId);

        if (cart is null)
        {
            return Result.Fail(
                new NotFoundError(
                    "Cart not found"));
        }

        var result = cart.RemoveItems(itemIds);

        if (result.IsFailed)
        {
            return result;
        }

        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        return Result.Ok();
    }

    public async Task<Result> ClearCartAsync(
        Guid? userId,
        Guid? cartId)
    {
        var cart = await GetCartEntityAsync(
            userId,
            cartId);

        if (cart is null)
        {
            return Result.Ok();
        }

        cart.ClearItems();

        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        return Result.Ok();
    }

    public async Task<Result> MergeCartsAsync(
        Guid userId,
        Guid anonymousCartId)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var userCart =
                await _unitOfWork.Carts
                    .GetByUserIdAsync(userId);

            var anonymousCart =
                await _unitOfWork.Carts
                    .GetByIdAsync(anonymousCartId);

            if (anonymousCart is null)
            {
                await _unitOfWork.CommitTransactionAsync();

                return Result.Ok();
            }

            // Корзина должна быть анонимной.
            if (anonymousCart.UserId.HasValue)
            {
                await _unitOfWork.RollbackTransactionAsync();

                return Result.Fail(
                    new CartStateError(
                        "Cart is already assigned to a user"));
            }

            // У пользователя нет своей корзины.
            // Просто передаём ему анонимную.
            if (userCart is null)
            {
                var assignResult =
                    anonymousCart.AssignToUser(userId);

                if (assignResult.IsFailed)
                {
                    await _unitOfWork.RollbackTransactionAsync();

                    return assignResult;
                }

                await _unitOfWork.Carts.UpdateAsync(
                    anonymousCart);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return Result.Ok();
            }

            // У пользователя уже есть корзина.
            // Добавляем в неё все товары из анонимной.
            foreach (var anonymousItem in anonymousCart.Items)
            {
                var itemResult = CartItem.Create(
                    userCart.CartId,
                    anonymousItem.ProductId,
                    anonymousItem.Quantity);

                if (itemResult.IsFailed)
                {
                    await _unitOfWork.RollbackTransactionAsync();

                    return Result.Fail(
                        itemResult.Errors);
                }

                var addResult = userCart.AddItem(
                    itemResult.Value);

                if (addResult.IsFailed)
                {
                    await _unitOfWork.RollbackTransactionAsync();

                    return addResult;
                }
            }

            await _unitOfWork.Carts.UpdateAsync(
                userCart);

            await _unitOfWork.Carts.DeleteAsync(
                anonymousCart);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation(
                "Merged anonymous cart {AnonymousCartId} " +
                "into user cart {UserCartId} for user {UserId}",
                anonymousCart.CartId,
                userCart.CartId,
                userId);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();

            _logger.LogError(
                ex,
                "Error merging cart {AnonymousCartId} " +
                "for user {UserId}",
                anonymousCartId,
                userId);

            return Result.Fail(
                new ExternalServiceError(
                    $"Не удалось объединить корзины: {ex.Message}"));
        }
    }
    
    
    

    private async Task<Result<Cart>> GetOrCreateCartAsync(
        Guid? userId,
        Guid? cartId)
    {
        var cart = await GetCartEntityAsync(
            userId,
            cartId);

        if (cart is not null)
        {
            return Result.Ok(cart);
        }

        // Новая анонимная корзина.
        if (!userId.HasValue && !cartId.HasValue)
        {
            cart = Cart.Create(null);
        }
        // Новая корзина авторизованного пользователя.
        else if (userId.HasValue)
        {
            cart = Cart.Create(userId);
        }
        // CartId был передан, но такой корзины нет.
        else
        {
            return Result.Fail<Cart>(
                new NotFoundError(
                    $"Cart {cartId} not found"));
        }

        await _unitOfWork.Carts.AddAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        return Result.Ok(cart);
    }

    private async Task<Cart?> GetCartEntityAsync(
        Guid? userId,
        Guid? cartId)
    {
        // Авторизованный пользователь.
        if (userId.HasValue)
        {
            return await _unitOfWork.Carts
                .GetByUserIdAsync(userId.Value);
        }

        // Анонимный пользователь.
        if (cartId.HasValue)
        {
            var cart =
                await _unitOfWork.Carts
                    .GetByIdAsync(cartId.Value);

            // Авторизованную корзину нельзя
            // использовать как анонимную.
            if (cart?.UserId is not null)
            {
                return null;
            }

            return cart;
        }

        return null;
    }

    private static CartDto MapToDto(Cart cart)
    {
        return new CartDto
        {
            Id = cart.CartId,
            UserId = cart.UserId,

            Items = cart.Items
                .Select(i => new CartItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                })
                .ToList(),

            UpdatedAt = cart.UpdatedAt
        };
    }
}

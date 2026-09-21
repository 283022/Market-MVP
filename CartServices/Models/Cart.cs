using FluentResults;

namespace CartServices.Models;

public class Cart
{
    public Guid CartId { get; private set; }
    public Guid? UserId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public List<CartItem> Items { get; private set; } = new();

    // Для EF Core
    private Cart()
    {
    }

    private Cart(Guid? userId)
    {
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Cart Create(Guid? userId)
    {
        if (userId == Guid.Empty)
            userId = null;

        return new Cart(userId);
    }

    public Result AssignToUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return Result.Fail(
                new ValidationError(
                    "User id cannot be empty"));
        }

        UserId = userId;
        UpdatedAt = DateTime.UtcNow;

        return Result.Ok();
    }

    public Result AddItem(CartItem item)
    {
        if (item is null)
        {
            return Result.Fail(
                new ValidationError(
                    "Item cannot be null"));
        }

        if (item.ProductId == Guid.Empty)
        {
            return Result.Fail(
                new ValidationError(
                    "Product id cannot be empty"));
        }

        var existingItem = Items.FirstOrDefault(
            i => i.ProductId == item.ProductId);

        if (existingItem is not null)
        {
            var result = existingItem.AddQuantity(
                item.Quantity);

            if (result.IsFailed)
                return result;
        }
        else
        {
            Items.Add(item);
        }

        UpdatedAt = DateTime.UtcNow;

        return Result.Ok();
    }

    public Result UpdateItemQuantity(
        Guid itemId,
        int quantity)
    {
        var item = Items.FirstOrDefault(
            i => i.Id == itemId);

        if (item is null)
        {
            return Result.Fail(
                new NotFoundError(
                    $"Item {itemId} not found in cart"));
        }

        if (quantity == 0)
        {
            Items.Remove(item);
            UpdatedAt = DateTime.UtcNow;

            return Result.Ok();
        }

        var result = item.UpdateQuantity(quantity);

        if (result.IsFailed)
            return result;

        UpdatedAt = DateTime.UtcNow;

        return Result.Ok();
    }

    public Result RemoveItem(Guid itemId)
    {
        var item = Items.FirstOrDefault(
            i => i.Id == itemId);

        if (item is null)
        {
            return Result.Fail(
                new NotFoundError(
                    $"Item {itemId} not found in cart"));
        }

        Items.Remove(item);
        UpdatedAt = DateTime.UtcNow;

        return Result.Ok();
    }

    public Result RemoveItems(IEnumerable<Guid> itemIds)
    {
        var ids = itemIds.ToHashSet();

        if (ids.Count == 0)
        {
            return Result.Fail(
                new ValidationError(
                    "Item ids must not be empty"));
        }

        var missingIds = ids
            .Except(Items.Select(i => i.Id))
            .ToList();

        if (missingIds.Count > 0)
        {
            return Result.Fail(
                missingIds.Select(id =>
                    (IError)new NotFoundError(
                        $"Item {id} not found in cart")));
        }

        Items.RemoveAll(i => ids.Contains(i.Id));
        UpdatedAt = DateTime.UtcNow;

        return Result.Ok();
    }

    public void ClearItems()
    {
        Items.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}


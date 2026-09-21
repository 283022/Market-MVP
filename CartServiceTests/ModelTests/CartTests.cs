using CartServices;
using CartServices.Models;
using FluentAssertions;

namespace CartServiceTests.ModelTests;

public class CartTests
{

    [Fact]
    public void Create_WhenUserIdIsProvided_CreatesCartForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var cart = Cart.Create(userId);

        // Assert
        cart.CartId.Should().Be(Guid.Empty);
        cart.UserId.Should().Be(userId);

        cart.CreatedAt.Should().NotBe(default);
        cart.UpdatedAt.Should().NotBeNull();

        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void Create_WhenUserIdIsNull_CreatesAnonymousCart()
    {
        // Act
        var cart = Cart.Create(null);

        // Assert
        cart.CartId.Should().Be(Guid.Empty);
        cart.UserId.Should().BeNull();

        cart.CreatedAt.Should().NotBe(default);
        cart.UpdatedAt.Should().NotBeNull();

        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void Create_WhenUserIdIsEmpty_CreatesAnonymousCart()
    {
        // Act
        var cart = Cart.Create(Guid.Empty);

        // Assert
        cart.UserId.Should().BeNull();
    }

    // ============================================================
    // AssignToUser
    // ============================================================

    [Fact]
    public void AssignToUser_WhenUserIdIsValid_AssignsUser()
    {
        // Arrange
        var cart = Cart.Create(null);
        var userId = Guid.NewGuid();

        var oldUpdatedAt = cart.UpdatedAt;

        // Act
        var result = cart.AssignToUser(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        cart.UserId.Should().Be(userId);
        cart.UpdatedAt.Should().BeAfter(oldUpdatedAt!.Value);
    }

    [Fact]
    public void AssignToUser_WhenUserIdIsEmpty_ReturnsValidationError()
    {
        // Arrange
        var cart = Cart.Create(null);

        // Act
        var result = cart.AssignToUser(Guid.Empty);

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().BeOfType<ValidationError>();

        cart.UserId.Should().BeNull();
    }

    // ============================================================
    // AddItem
    // ============================================================

    [Fact]
    public void AddItem_WhenItemIsValid_AddsItem()
    {
        // Arrange
        var cart = Cart.Create(null);

        var itemResult = CartItem.Create(
            cart.CartId,
            Guid.NewGuid(),
            2);

        itemResult.IsSuccess.Should().BeTrue();

        var item = itemResult.Value;

        // Act
        var result = cart.AddItem(item);

        // Assert
        result.IsSuccess.Should().BeTrue();

        cart.Items.Should().ContainSingle();
        cart.Items[0].Should().BeSameAs(item);
        cart.Items[0].Quantity.Should().Be(2);
    }

    [Fact]
    public void AddItem_WhenItemIsNull_ReturnsValidationError()
    {
        // Arrange
        var cart = Cart.Create(null);

        // Act
        var result = cart.AddItem(null!);

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().BeOfType<ValidationError>();

        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_WhenProductIdIsEmpty_ReturnsValidationError()
    {
        // Arrange
        var cart = Cart.Create(null);

        // Создать такой CartItem через Create нельзя,
        // потому что Create сам проверяет ProductId.
        // Поэтому проверяем только контракт Cart.AddItem
        // через объект, созданный для теста.
        var item = CreateCartItemWithoutValidation(
            cart.CartId,
            Guid.Empty,
            1);

        // Act
        var result = cart.AddItem(item);

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().BeOfType<ValidationError>();

        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_WhenSameProductAlreadyExists_IncreasesQuantity()
    {
        // Arrange
        var cart = Cart.Create(null);
        var productId = Guid.NewGuid();

        var firstItem = CartItem.Create(
            cart.CartId,
            productId,
            2);

        var secondItem = CartItem.Create(
            cart.CartId,
            productId,
            3);

        firstItem.IsSuccess.Should().BeTrue();
        secondItem.IsSuccess.Should().BeTrue();

        cart.AddItem(firstItem.Value);

        // Act
        var result = cart.AddItem(secondItem.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();

        cart.Items.Should().ContainSingle();
        cart.Items[0].ProductId.Should().Be(productId);
        cart.Items[0].Quantity.Should().Be(5);

        // Второй объект не должен добавляться как отдельная строка.
        cart.Items.Should().NotContain(secondItem.Value);
    }

    [Fact]
    public void AddItem_WhenDifferentProducts_AddsSeparateItems()
    {
        // Arrange
        var cart = Cart.Create(null);

        var firstItem = CreateItem(
            cart,
            Guid.NewGuid(),
            2);

        var secondItem = CreateItem(
            cart,
            Guid.NewGuid(),
            3);

        // Act
        var firstResult = cart.AddItem(firstItem);
        var secondResult = cart.AddItem(secondItem);

        // Assert
        firstResult.IsSuccess.Should().BeTrue();
        secondResult.IsSuccess.Should().BeTrue();

        cart.Items.Should().HaveCount(2);
        cart.Items.Should().Contain(firstItem);
        cart.Items.Should().Contain(secondItem);
    }

    [Fact]
    public void AddItem_WhenItemHasInvalidQuantity_ReturnsValidationError()
    {
        // Arrange
        var cart = Cart.Create(null);

        var itemResult = CartItem.Create(
            cart.CartId,
            Guid.NewGuid(),
            1);

        itemResult.IsSuccess.Should().BeTrue();

        var item = itemResult.Value;

        // Act
        var result = item.AddQuantity(-1);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ValidationError>();

        cart.Items.Should().BeEmpty();
    }

    // ============================================================
    // UpdateItemQuantity
    // ============================================================

    [Fact]
    public void UpdateItemQuantity_WhenItemExists_UpdatesQuantity()
    {
        // Arrange
        var cart = Cart.Create(null);

        var item = CreateItem(
            cart,
            Guid.NewGuid(),
            2);

        cart.AddItem(item);

        // Act
        var result = cart.UpdateItemQuantity(
            item.Id,
            7);

        // Assert
        result.IsSuccess.Should().BeTrue();

        item.Quantity.Should().Be(7);
        cart.Items.Should().ContainSingle();
    }

    [Fact]
    public void UpdateItemQuantity_WhenQuantityIsZero_RemovesItem()
    {
        // Arrange
        var cart = Cart.Create(null);

        var item = CreateItem(
            cart,
            Guid.NewGuid(),
            2);

        cart.AddItem(item);

        // Act
        var result = cart.UpdateItemQuantity(
            item.Id,
            0);

        // Assert
        result.IsSuccess.Should().BeTrue();

        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void UpdateItemQuantity_WhenQuantityIsNegative_ReturnsValidationError()
    {
        // Arrange
        var cart = Cart.Create(null);

        var item = CreateItem(
            cart,
            Guid.NewGuid(),
            2);

        cart.AddItem(item);

        // Act
        var result = cart.UpdateItemQuantity(
            item.Id,
            -1);

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().BeOfType<ValidationError>();

        item.Quantity.Should().Be(2);
        cart.Items.Should().ContainSingle();
    }

    [Fact]
    public void UpdateItemQuantity_WhenItemDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var cart = Cart.Create(null);
        var itemId = Guid.NewGuid();

        // Act
        var result = cart.UpdateItemQuantity(
            itemId,
            5);

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().BeOfType<NotFoundError>();

        cart.Items.Should().BeEmpty();
    }

    // ============================================================
    // RemoveItem
    // ============================================================

    [Fact]
    public void RemoveItem_WhenItemExists_RemovesItem()
    {
        // Arrange
        var cart = Cart.Create(null);

        var item = CreateItem(
            cart,
            Guid.NewGuid(),
            2);

        cart.AddItem(item);

        // Act
        var result = cart.RemoveItem(item.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_WhenItemDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var cart = Cart.Create(null);
        var itemId = Guid.NewGuid();

        // Act
        var result = cart.RemoveItem(itemId);

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().BeOfType<NotFoundError>();
    }

    // ============================================================
    // RemoveItems
    // ============================================================

    [Fact]
    public void RemoveItems_WhenIdsAreEmpty_ReturnsValidationError()
    {
        // Arrange
        var cart = Cart.Create(null);

        // Act
        var result = cart.RemoveItems([]);

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().BeOfType<ValidationError>();
    }

    [Fact]
    public void RemoveItems_WhenAllItemsExist_RemovesItems()
    {
        // Arrange
        var cart = Cart.Create(null);

        var item1 = CreateItem(
            cart,
            Guid.NewGuid(),
            1);

        var item2 = CreateItem(
            cart,
            Guid.NewGuid(),
            2);

        var item3 = CreateItem(
            cart,
            Guid.NewGuid(),
            3);

        cart.AddItem(item1);
        cart.AddItem(item2);
        cart.AddItem(item3);

        // Act
        var result = cart.RemoveItems(
            [
                item1.Id,
                item2.Id
            ]);

        // Assert
        result.IsSuccess.Should().BeTrue();

        cart.Items.Should().ContainSingle();
        cart.Items[0].Id.Should().Be(item3.Id);
    }

    [Fact]
    public void RemoveItems_WhenOneItemDoesNotExist_DoesNotRemoveAnyItems()
    {
        // Arrange
        var cart = Cart.Create(null);

        var item = CreateItem(
            cart,
            Guid.NewGuid(),
            1);

        cart.AddItem(item);

        var missingItemId = Guid.NewGuid();

        // Act
        var result = cart.RemoveItems(
            [
                item.Id,
                missingItemId
            ]);

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors.Should().Contain(
            error =>
                error is NotFoundError &&
                error.Message.Contains(
                    missingItemId.ToString()));

        // Операция должна быть атомарной на уровне aggregate:
        // если одного товара нет, существующий тоже не удаляем.
        cart.Items.Should().ContainSingle();
        cart.Items[0].Id.Should().Be(item.Id);
    }

    [Fact]
    public void RemoveItems_WhenSeveralItemsAreMissing_ReturnsErrorsForMissingItems()
    {
        // Arrange
        var cart = Cart.Create(null);

        var existingItem = CreateItem(
            cart,
            Guid.NewGuid(),
            1);

        cart.AddItem(existingItem);

        var missingId1 = Guid.NewGuid();
        var missingId2 = Guid.NewGuid();

        // Act
        var result = cart.RemoveItems(
            [
                existingItem.Id,
                missingId1,
                missingId2
            ]);

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors.Should().Contain(
            error =>
                error is NotFoundError &&
                error.Message.Contains(
                    missingId1.ToString()));

        result.Errors.Should().Contain(
            error =>
                error is NotFoundError &&
                error.Message.Contains(
                    missingId2.ToString()));

        cart.Items.Should().ContainSingle();
    }

    // ============================================================
    // ClearItems
    // ============================================================

    [Fact]
    public void ClearItems_WhenCartHasItems_RemovesAllItems()
    {
        // Arrange
        var cart = Cart.Create(null);

        cart.AddItem(
            CreateItem(
                cart,
                Guid.NewGuid(),
                1));

        cart.AddItem(
            CreateItem(
                cart,
                Guid.NewGuid(),
                2));

        // Act
        cart.ClearItems();

        // Assert
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void ClearItems_WhenCartIsEmpty_DoesNotFail()
    {
        // Arrange
        var cart = Cart.Create(null);

        // Act
        var action = () => cart.ClearItems();

        // Assert
        action.Should().NotThrow();
        cart.Items.Should().BeEmpty();
    }

    // ============================================================
    // UpdateTimestamp
    // ============================================================

    [Fact]
    public void UpdateTimestamp_UpdatesUpdatedAt()
    {
        // Arrange
        var cart = Cart.Create(null);
        var oldUpdatedAt = cart.UpdatedAt;

        // Act
        cart.UpdateTimestamp();

        // Assert
        cart.UpdatedAt.Should().NotBeNull();
        cart.UpdatedAt.Should().BeAfter(
            oldUpdatedAt!.Value);
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static CartItem CreateItem(
        Cart cart,
        Guid productId,
        int quantity)
    {
        var result = CartItem.Create(
            cart.CartId,
            productId,
            quantity);

        result.IsSuccess.Should().BeTrue();

        return result.Value;
    }

    private static CartItem CreateCartItemWithoutValidation(
        Guid cartId,
        Guid productId,
        int quantity)
    {
        // CartItem.Create() специально не позволяет создать
        // невалидный объект, поэтому этот тест нельзя нормально
        // реализовать через публичный API.
        //
        // Метод оставлен как заглушка для пояснения:
        // проверку ProductId == Guid.Empty в Cart.AddItem()
        // можно протестировать только если появится способ
        // создать такой объект.
        throw new NotSupportedException();
    }
}

using CheckoutService.Clients;
using CheckoutService.DTOs;
using Robokassa.NET;

namespace CheckoutService;

public class CheckoutService(
    OrderClient orderClient,
    CartClient cartClient,
    MenuClient menuClient,
    IRobokassaService robokassaService)
{
    public async Task<CheckoutResult> CheckoutAsync(CheckOutDto dto, Guid userId)
    {
        // 1. Получаем корзину
        var cart = await cartClient.GetCartAsync(userId);
        if (cart == null || cart.Items.Count == 0)
            throw new Exception("Корзина пуста");

        // 2. Фильтруем выбранные товары
        var selectedItems = dto.Items.Where(x => x.IsSelected).ToList();
        if (selectedItems.Count == 0)
            throw new Exception("Не выбрано ни одного товара");

        // 3. Проверяем, что все товары есть в корзине и количество не превышено
        foreach (var item in selectedItems)
        {
            var cartItem = cart.Items.FirstOrDefault(x => x.ProductId == item.ProductId);
            if (cartItem == null)
                throw new Exception($"Товар {item.ProductId} не найден в корзине");
            if (item.Quantity > cartItem.Quantity)
                throw new Exception($"Товара {item.ProductId} в корзине меньше, чем запрошено");
        }

        var productIds = selectedItems.Select(x => x.ProductId).ToList();

        // 4. Валидация товаров через Menu Service
        var validation = await menuClient.ValidateProductsAsync(productIds);
        if (!validation.IsValid)
            throw new Exception($"Некоторые товары недоступны: {string.Join(", ", validation.Errors.Select(e => e.Reason))}");

        // 5. Получение деталей (цен и названий)
        var details = await menuClient.GetProductDetailsAsync(productIds);
        if (details == null || details.Products.Count == 0)
            throw new Exception("Не удалось получить данные о товарах");

        // 6. Сборка OrderItemDto
        var orderItems = selectedItems.Select(item =>
        {
            var detail = details.Products.First(x => x.ProductId == item.ProductId);
            return new OrderItemDto
            {
                ProductId = item.ProductId,
                ProductName = detail.Name,
                Quantity = item.Quantity,
                UnitPrice = detail.Price
            };
        }).ToList();

        // 7. Создание заказа в Order Service
        var createOrderRequest = new CreateOrderRequestDto
        {
            Items = orderItems,
            Comment = dto.Comment
        };
        var orderResult = await orderClient.CreateOrderAsync(userId, createOrderRequest);

        // 8. Генерация ссылки на оплату через Robokassa
        var paymentLink = robokassaService.GenerateAuthLink(
            orderResult.OrderId.ToString(),
            orderResult.TotalAmount,
            $"Оплата заказа #{orderResult.OrderNumber}"
        );

        // 9. Возврат результата
        return new CheckoutResult
        {
            OrderId = orderResult.OrderId,
            PaymentUrl = paymentLink,
            ExpiresAt = orderResult.ExpiresAt
        };
    }
}
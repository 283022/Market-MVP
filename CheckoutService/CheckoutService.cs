using CheckoutService.Clients;
using CheckoutService.DTOs;
using FluentResults;
using Robokassa.NET;

namespace CheckoutService;

public class CheckoutService(
    OrderClient orderClient,
    CartClient cartClient,
    MenuClient menuClient,
    IRobokassaService robokassaService)
{
    public async Task<Result<CheckoutResult>> CheckoutAsync(CheckOutDto dto, Guid userId)
    {
        // 1. Получаем корзину
        var cartResult = await cartClient.GetCartAsync(userId);
        if (cartResult.IsFailed)
            return Result.Fail<CheckoutResult>(cartResult.Errors);

        var cart = cartResult.Value;
        if (cart.Items is null || cart.Items.Count == 0)
            return Result.Fail<CheckoutResult>(new CartError("Корзина пуста"));

        // 2. Фильтруем выбранные товары
        var selectedItems = dto.Items.Where(x => x.IsSelected).ToList();
        if (selectedItems.Count == 0)
            return Result.Fail<CheckoutResult>(
                new ValidationError("Не выбрано ни одного товара"));

        // 3–4. Проверяем, что все товары есть в корзине и количество не превышено.
        //      Собираем ВСЕ ошибки, а не только первую.
        var cartErrors = new List<IError>();
        foreach (var item in selectedItems)
        {
            var cartItem = cart.Items.FirstOrDefault(x => x.ProductId == item.ProductId);
            if (cartItem is null)
            {
                cartErrors.Add(new CartError(
                    $"Товар {item.ProductId} не найден в корзине"));
                continue;
            }

            if (item.Quantity > cartItem.Quantity)
            {
                cartErrors.Add(new CartError(
                    $"Товара {item.ProductId} в корзине меньше, чем запрошено " +
                    $"(доступно {cartItem.Quantity}, запрошено {item.Quantity})"));
            }
        }

        if (cartErrors.Any())
            return Result.Fail<CheckoutResult>(cartErrors);

        var productIds = selectedItems.Select(x => x.ProductId).ToList();

        // 5. Валидация товаров через Menu Service
        var validationResult = await menuClient.ValidateProductsAsync(productIds);
        if (validationResult.IsFailed)
            return Result.Fail<CheckoutResult>(validationResult.Errors);

        var validation = validationResult.Value;
        if (!validation.IsValid)
        {
            var menuErrors = validation.Errors
                .Select(e => (IError)new ValidationError(
                    $"Товар недоступен: {e.Reason}"))
                .ToList();

            return Result.Fail<CheckoutResult>(menuErrors);
        }

        // 6. Получение деталей (цен и названий)
        var detailsResult = await menuClient.GetProductDetailsAsync(productIds);
        if (detailsResult.IsFailed)
            return Result.Fail<CheckoutResult>(detailsResult.Errors);

        var details = detailsResult.Value;
        if (details.Products is null || details.Products.Count == 0)
            return Result.Fail<CheckoutResult>(
                new ExternalServiceError("Не удалось получить данные о товарах"));

        // 7. Сборка OrderItemDto. Если какой-то детали нет — сообщаем обо всех таких сразу.
        var missingDetails = productIds
            .Where(id => details.Products.All(p => p.ProductId != id))
            .ToList();

        if (missingDetails.Any())
        {
            return Result.Fail<CheckoutResult>(
                missingDetails.Select(id =>
                    (IError)new ExternalServiceError(
                        $"Нет данных о товаре {id}"))
                .ToList());
        }

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

        // 8. Создание заказа в Order Service
        var createOrderRequest = new CreateOrderRequestDto
        {
            Items = orderItems,
            Comment = dto.Comment
        };

        var orderResult = await orderClient.CreateOrderAsync(userId, createOrderRequest);
        if (orderResult.IsFailed)
            return Result.Fail<CheckoutResult>(orderResult.Errors);

        var order = orderResult.Value;

        // 9. Генерация ссылки на оплату через Robokassa
        // TODO: сделать нормальную генерацию ссылки
        string paymentLink;
        try
        {
            paymentLink = robokassaService.GenerateAuthLink(
                order.OrderId.ToString(),
                order.TotalAmount,
                $"Оплата заказа #{order.OrderNumber}");
        }
        catch (Exception ex)
        {
            return Result.Fail<CheckoutResult>(
                new PaymentError($"Не удалось сгенерировать ссылку на оплату: {ex.Message}"));
        }

        // 10. Возврат результата
        return Result.Ok(new CheckoutResult
        {
            OrderId = order.OrderId,
            PaymentUrl = paymentLink,
            ExpiresAt = order.ExpiresAt
        });
    }
}
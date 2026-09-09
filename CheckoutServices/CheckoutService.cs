using CheckoutService.Models;
using Microsoft.AspNetCore.Http.Features;

namespace CheckoutService;

public class CheckoutService(
    OrderClient order,
    CartClient cart,
    MenuClient menu)
{
    private readonly OrderClient _orderClient = order;
    private readonly CartClient _cartClient = cart;
    private readonly MenuClient _menuClient = menu;

    public async Task Checkout(CheckOutDto dto, Guid userId)
    {
        //сбор id
        HashSet<Guid> productId = [];
        foreach (var item in dto.Items)
        {
            productId.Add(item.ProductId);
        }
        //проверка валидности корзины
        var resultCart = _cartClient.ValidCart(productId);
        if (resultCart.Fail)
            throw new Exception(resultCart.Error);

        //проверка валидности каждого товара
        var resultMenu = _menuClient.ValidProduct(productId);
        if (resultMenu.Fail)
            throw new Exception(resultMenu.Error);
        
        //получение цены каждого товара
        var resultPrice = _menuClient.GetPriceById(productId);
        if (resultPrice.Fail)
            throw new Exception(resultPrice.Error);

        List<Item> items = [];
        //создание сущностей
        foreach (var id in resultCart.Keys)
        {
            Item.Create(id, resultCart[id], resultPrice.Value[id]);
        }

        var order = Order.Create(items);
        
        var resultOrder = _orderClient.CreateOrder(order);
        if(resultOrder.Fail)
            throw new Exception("Order Failed");
        
    }
}
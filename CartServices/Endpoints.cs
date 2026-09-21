using System.Security.Claims;
using CartServices.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;

namespace CartServices;

public static class Endpoints
{
    public static WebApplication AddEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/cart");

        // Post /api/cart/create - создать корзину
        group.MapPost("/create",
            async (HttpContext context, CartService service) =>
            {
                var (userId, cartId) = Extract(context);
                var result = await service.CreateAsync(userId,cartId);
                return result.ToHttpResult();
            });
        
        // GET /api/cart/my — получить корзину
        group.MapGet("/my", async (
            HttpContext context,
            CartService service) =>
        {
            var (userId, cartId) = Extract(context);

            var result = await service.GetCartAsync(userId, cartId);

            return result.ToHttpResult();
        });

        // GET /api/cart/count — количество товаров в корзине
        group.MapGet("/count", async (
            HttpContext context,
            CartService service) =>
        {
            var (userId, cartId) = Extract(context);

            var result = await service.GetCartCountAsync(userId, cartId);

            return result.ToHttpResult(
                count => Results.Ok(new { count }));
        });

        // POST /api/cart/items — добавить товар
        group.MapPost("/items", async (
            AddCartItemRequest request,
            HttpContext context,
            CartService service) =>
        {
            var (userId, cartId) = Extract(context);

            var result = await service.AddItemAsync(
                userId,
                cartId,
                request);

            return result.ToHttpResult();
        });

        // PUT /api/cart/items/{id} — обновить количество
        group.MapPut("/items/{id:guid}", async (
            Guid id,
            UpdateCartItemRequest request,
            HttpContext context,
            CartService service) =>
        {
            var (userId, cartId) = Extract(context);

            var result = await service.UpdateItemQuantityAsync(
                userId,
                cartId,
                id,
                request.Quantity);

            return result.ToHttpResult(
                () => Results.NoContent());
        });

        // DELETE /api/cart/items/{id} — удалить товар
        group.MapDelete("/items/{id:guid}", async (
            Guid id,
            HttpContext context,
            CartService service) =>
        {
            var (userId, cartId) = Extract(context);

            var result = await service.RemoveItemAsync(
                userId,
                cartId,
                id);

            return result.ToHttpResult(
                () => Results.NoContent());
        });

        // DELETE /api/cart/items — удалить несколько товаров
        group.MapDelete("/items", async (
            [FromBody] RemoveItemsRequest request,
            HttpContext context,
            CartService service) =>
        {
            var (userId, cartId) = Extract(context);

            var result = await service.RemoveItemsAsync(
                userId,
                cartId,
                request.ItemIds);

            return result.ToHttpResult(
                () => Results.NoContent());
        });

        // DELETE /api/cart/clear — очистить корзину
        group.MapDelete("/clear", async (
            HttpContext context,
            CartService service) =>
        {
            var (userId, cartId) = Extract(context);

            var result = await service.ClearCartAsync(
                userId,
                cartId);

            return result.ToHttpResult(
                () => Results.NoContent());
        });

        // POST /api/cart/merge — объединить анонимную корзину
        // с корзиной авторизованного пользователя
        group.MapPost("/merge", async (
            HttpContext context,
            CartService service) =>
        {
            var (userId, cartId) = Extract(context);

            if (userId is null)
                return Results.Unauthorized();

            if (cartId is null)
                return Results.BadRequest("Cart id is required");

            var result = await service.MergeCartsAsync(
                userId.Value,
                cartId.Value);

            return result.ToHttpResult(
                () => Results.NoContent());
        })
        .RequireAuthorization(JwtBearerDefaults.AuthenticationScheme);

        return app;
    }

    private static (Guid? UserId, Guid? CartId) Extract(
        HttpContext context)
    {
        var userIdClaim =
            context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        Guid? userId =
            Guid.TryParse(userIdClaim, out var parsedUserId)
            && parsedUserId != Guid.Empty
                ? parsedUserId
                : null;

        Guid? cartId = null;

        if (context.Request.Cookies.TryGetValue("CartId", out var value)
            && Guid.TryParse(value, out var parsedCartId)
            && parsedCartId != Guid.Empty)
        {
            cartId = parsedCartId;
        }

        return (userId, cartId);
    }
}

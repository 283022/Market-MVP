using System.Security.Claims;
using CartServices.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;

namespace CartServices;

public static class Endpoints
{
    public static WebApplication AddEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/cart");

        // GET /api/cart/my — получить корзину
        group.MapGet("/my", async (
            HttpContext context,
            CartService service) =>
        {
            var userId = GetUserId(context);
            var sessionId = context.Items["CartSessionId"]?.ToString();

            var cart = await service.GetCartAsync(userId, sessionId);
            return Results.Ok(cart);
        });

        // GET /api/cart/count — получить количество товаров в корзине
        group.MapGet("/count", async (
            HttpContext context,
            CartService service) =>
        {
            var userId = GetUserId(context);
            var sessionId = context.Items["CartSessionId"]?.ToString();

            var count = await service.GetCartCountAsync(userId, sessionId);
            return Results.Ok(new { count });
        });

        // POST /api/cart/items — добавить товар
        group.MapPost("/items", async (
            AddCartItemRequest request,
            HttpContext context,
            CartService service) =>
        {
            var userId = GetUserId(context);
            var sessionId = context.Items["CartSessionId"]?.ToString();

            var cart = await service.AddItemAsync(userId, sessionId, request);
            return Results.Ok(cart);
        });

        // PUT /api/cart/items/{id} — обновить количество
        group.MapPut("/items/{id:guid}", async (
            Guid id,
            UpdateCartItemRequest request,
            HttpContext context,
            CartService service) =>
        {
            var userId = GetUserId(context);
            var sessionId = context.Items["CartSessionId"]?.ToString();

            await service.UpdateItemQuantityAsync(userId, sessionId, id, request.Quantity);
            return Results.Ok();
        });

        // DELETE /api/cart/items/{id} — удалить товар
        group.MapDelete("/items/{id:guid}", async (
            Guid id,
            HttpContext context,
            CartService service) =>
        {
            var userId = GetUserId(context);
            var sessionId = context.Items["CartSessionId"]?.ToString();

            await service.RemoveItemAsync(userId, sessionId, id);
            return Results.Ok();
        });

        // DELETE /api/cart/items — удалить несколько товаров (выбор части корзины)
        group.MapDelete("/items", async (
            [FromBody] RemoveItemsRequest request,
            HttpContext context,
            CartService service) =>
        {
            var userId = GetUserId(context);
            var sessionId = context.Items["CartSessionId"]?.ToString();

            await service.RemoveItemsAsync(userId, sessionId, request.ItemIds);
            return Results.Ok();
        });


        // DELETE /api/cart/clear — очистить корзину
        group.MapDelete("/clear", async (
            HttpContext context,
            CartService service) =>
        {
            var userId = GetUserId(context);
            var sessionId = context.Items["CartSessionId"]?.ToString();

            await service.ClearCartAsync(userId, sessionId);
            return Results.NoContent();
        });
        
        // POST /api/cart/merge — объединить анонимную корзину с пользовательской
        group.MapPost("/merge", async (
            HttpContext context,
            CartService service) =>
        {
            var userId = GetUserId(context);
            var sessionId = context.Items["CartSessionId"]?.ToString();

            if (userId == null)
                return Results.Unauthorized();

            await service.MergeCartsAsync(userId.Value, sessionId);
            return Results.Ok();
        }).RequireAuthorization(JwtBearerDefaults.AuthenticationScheme); 

        return app;
    }

    private static Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
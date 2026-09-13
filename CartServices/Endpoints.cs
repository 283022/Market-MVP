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
        group.MapGet("/my", async (HttpContext context, CartService service) =>
        {
            var (userId, sessionId) = Extract(context);
            var result = await service.GetCartAsync(userId, sessionId);
            return result.ToHttpResult();
        });

        // GET /api/cart/count — количество товаров в корзине
        group.MapGet("/count", async (HttpContext context, CartService service) =>
        {
            var (userId, sessionId) = Extract(context);
            var result = await service.GetCartCountAsync(userId, sessionId);

            return result.ToHttpResult(count => Results.Ok(new { count }));
        });

        // POST /api/cart/items — добавить товар
        group.MapPost("/items", async (
            AddCartItemRequest request, HttpContext context, CartService service) =>
        {
            var (userId, sessionId) = Extract(context);
            var result = await service.AddItemAsync(userId, sessionId, request);
            return result.ToHttpResult();
        });

        // PUT /api/cart/items/{id} — обновить количество
        group.MapPut("/items/{id:guid}", async (
            Guid id, UpdateCartItemRequest request, HttpContext context, CartService service) =>
        {
            var (userId, sessionId) = Extract(context);
            var result = await service.UpdateItemQuantityAsync(
                userId, sessionId, id, request.Quantity);

            return result.ToHttpResult(() => Results.NoContent());
        });

        // DELETE /api/cart/items/{id} — удалить товар
        group.MapDelete("/items/{id:guid}", async (
            Guid id, HttpContext context, CartService service) =>
        {
            var (userId, sessionId) = Extract(context);
            var result = await service.RemoveItemAsync(userId, sessionId, id);

            return result.ToHttpResult(() => Results.NoContent());
        });

        // DELETE /api/cart/items — удалить несколько товаров
        group.MapDelete("/items", async (
            [FromBody] RemoveItemsRequest request, HttpContext context, CartService service) =>
        {
            var (userId, sessionId) = Extract(context);
            var result = await service.RemoveItemsAsync(
                userId, sessionId, request.ItemIds);

            return result.ToHttpResult(() => Results.NoContent());
        });

        // DELETE /api/cart/clear — очистить корзину
        group.MapDelete("/clear", async (HttpContext context, CartService service) =>
        {
            var (userId, sessionId) = Extract(context);
            var result = await service.ClearCartAsync(userId, sessionId);

            return result.ToHttpResult(() => Results.NoContent());
        });

        // POST /api/cart/merge — объединить анонимную корзину с пользовательской
        group.MapPost("/merge", async (HttpContext context, CartService service) =>
        {
            var (userId, sessionId) = Extract(context);

            if (userId is null)
                return Results.Unauthorized();

            var result = await service.MergeCartsAsync(userId.Value, sessionId);
            return result.ToHttpResult(() => Results.NoContent());
        }).RequireAuthorization(JwtBearerDefaults.AuthenticationScheme);

        return app;
    }

    private static (Guid? UserId, string? SessionId) Extract(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = Guid.TryParse(userIdClaim, out var parsed) && parsed != Guid.Empty
            ? parsed
            : null;

        var sessionId = context.Items["CartSessionId"]?.ToString();
        return (userId, sessionId);
    }
}
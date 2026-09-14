using Microsoft.AspNetCore.Mvc;

using OrderServices.Services;

namespace OrderServices;

public static class Endpoints
{
    public static WebApplication BuildWebApplication(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders");

        group.MapGet("/my", async (OrderService order, HttpContext context) =>
        {
            var id = GetId(context);
            if (id is null)
                return Results.Unauthorized();

            var result = await order.GetUserOrdersById(id.Value);
            return result.ToHttpResult();
        }).RequireAuthorization();

        group.MapPost("/create",
            async (HttpContext context, OrderService order, CreateOrderRequest dto) =>
            {
                var id = GetId(context);
                if (id is null)
                    return Results.Unauthorized();

                var result = await order.CreateOrderAsync(id.Value, dto);
                return result.ToHttpResult();
            }).RequireAuthorization();

        group.MapGet("/{id:guid}",
            async ([FromRoute] Guid id, OrderService order, HttpContext context) =>
            {
                var userId = GetId(context);
                if (userId is null)
                    return Results.Unauthorized();

                var result = await order.GetOrderById(id, userId.Value);
                return result.ToHttpResult();
            }).RequireAuthorization();

        group.MapPatch("/{id:guid}/cancel",
            async ([FromRoute] Guid id, HttpContext context, OrderService order) =>
            {
                var userId = GetId(context);
                if (userId is null)
                    return Results.Unauthorized();

                var result = await order.CancelOrder(id, userId.Value);
                return result.ToHttpResult();
            }).RequireAuthorization();

        group.MapPost("/{id:guid}/payment-confirm",
            async ([FromRoute] Guid id, HttpContext context, OrderService order) =>
            {
                var result = await order.PaymentConfirm(id);
                return result.ToHttpResult();
            });

        group.MapGet("/{id:guid}/status",
            async ([FromRoute] Guid id, HttpContext context, OrderService order) =>
            {
                var userId = GetId(context);
                if (userId is null)
                    return Results.Unauthorized();

                var result = await order.GetStatus(id, userId.Value);
                return result.ToHttpResult();
            }).RequireAuthorization();

        return app;
    }

    public static Guid? GetId(HttpContext context)
    {
        var raw = context.Items["userid"];
        if (Guid.TryParse(raw?.ToString(), out var userId) && userId != Guid.Empty)
            return userId;
        return null;
    }
}
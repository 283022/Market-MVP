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
            if (id == null)
                return Results.Unauthorized();
            var result = await order.GetUserOrdersById(id);
            if (result.Fail)
                return Results.BadRequest();

            return Results.Ok(result);
        }).RequireAuthorization();
        
        group.MapPost("/create", 
            async (HttpContext context,
                OrderService order, CreateOrderRequest dto) =>
        {
            var id = GetId(context);
            if (id == null)
                return Results.Unauthorized();
            var result = await order.CreateOrderAsync(id, dto);
            
            if (result.Fail)
                return Results.BadRequest();
            return Results.Ok(result);
        }).RequireAuthorization();
        
        group.MapGet("/{id}",
                async ([FromRoute] Guid orderId,OrderService order, HttpContext context) =>
            {
                var userid = GetId(context);
                if (userid == null)
                    return Results.Unauthorized();

                var result = await order.GetOrderById(orderId, userid);
                
                if (result.Fail)
                    return Results.BadRequest();
                return Results.Ok(result);
            })
            .RequireAuthorization();

        group.MapPatch("/{id:guid}/cancel",
           async (HttpContext context, OrderService order) =>
        {
            var userId = GetId(context);
            if (userId == null)
                return Results.Unauthorized();

            var result = await order.CancelOrder();
            
            if (result.Fail)
                return Results.BadRequest();
            return Results.Ok(result);
        });

        group.MapPost("/{id:guid}/payment-confirm",
            (HttpContext context, OrderService order) =>
            {
                var IsAnotherService = true;
                var result = await order.ConfirmPayment();
                if (result.Fail)
                    return Results.BadRequest();
                return Results.Ok();
            });

        group.MapGet("/{id:guid}/status",
            async ([FromRoute] Guid id,HttpContext context, OrderService order) =>
            {
                var userid = GetId(context);
                if (userid == null)
                    return Results.Unauthorized();

                var result = await order.GetStatus(id, userid);
                
                if (result.Fail)
                    return Results.BadRequest();
                return Results.Ok(result);
            });
        
        
        return app;
    }

    public static Guid? GetId(HttpContext context)
    {
        var id = context.Items["userid"];
        Guid.TryParse(id?.ToString(), out var userId);
        if (userId == Guid.Empty || userId == null)
            return null;
        return userId;
    }
}
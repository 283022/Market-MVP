using CheckoutService;
using CheckoutService.Clients;
using CheckoutService.DTOs;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

//TODO: сделать ретраи и продумать как это будет реализовано по потоку
builder.Services.AddHttpClient<CartClient>();
builder.Services.AddHttpClient<MenuClient>();
builder.Services.AddHttpClient<OrderClient>();
builder.Services.AddScoped<CheckoutService.CheckoutService>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/api/checkout",
        async (CheckoutService.CheckoutService service,
            HttpContext context,
            [FromBody] CheckOutDto dto) =>
        {
            var raw = context.Items["userid"];
            if (!Guid.TryParse(raw?.ToString(), out var userId) || userId == Guid.Empty)
                return Results.Unauthorized();

            var result = await service.CheckoutAsync(dto, userId);
            return result.ToHttpResult();
        })
    .RequireAuthorization()
    .WithName("Checkout");

app.Run();
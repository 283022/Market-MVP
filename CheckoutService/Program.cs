using CheckoutService;
using CheckoutService.Clients;
using CheckoutService.DTOs;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

a
builder.Services.AddHttpClient<CartClient>();
builder.Services.AddHttpClient<MenuClient>();
builder.Services.AddHttpClient<OrderClient>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("api/checkout",
        async (CheckoutService.CheckoutService service,
            HttpContext context,[FromBody] CheckOutDto dto) =>
    {
        var id = context.Items["userid"];
        Guid.TryParse(id?.ToString(), out var userId);
        if (userId == Guid.Empty || userId == null)
            return Results.BadRequest();

        var result = await service.CheckoutAsync(dto, userId);
        
        return Results.Ok();
    })
    .WithName("GetWeatherForecast");

app.Run();

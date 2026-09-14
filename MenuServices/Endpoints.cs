using MenuServices.DTOs;
using MenuServices.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuServices;

public static class Endpoints
{
    public static WebApplication AddEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/menu");

        // GET /api/menu — получить меню с пагинацией и фильтрацией
        group.MapGet("/", async (
            [AsParameters] MenuQueryParams queryParams,
            ProductService service) =>
        {
            var result = await service.GetMenuAsync(queryParams);
            return result.ToHttpResult();
        });

        // GET /api/menu/{id} — получить товар по ID
        group.MapGet("/{id:int}", async (
            int id,
            ProductService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.ToHttpResult();
        });

        // POST /api/menu — создать новый товар
        group.MapPost("/", async (
            CreateProductDto dto,
            ProductService service) =>
        {
            var result = await service.CreateAsync(dto);

            return result.ToHttpResult(product =>
                Results.Created($"/api/menu/{product.Id}", product));
        }).RequireAuthorization("Admin");

        // PATCH /api/menu/{id} — обновить товар
        group.MapPatch("/{id:int}", async (
            int id,
            UpdateProductDto dto,
            ProductService service) =>
        {
            var result = await service.UpdateAsync(id, dto);
            return result.ToHttpResult();
        }).RequireAuthorization("AdminOrCurator");

        // DELETE /api/menu/{id} — удалить товар
        group.MapDelete("/{id:int}", async (
            int id,
            ProductService service) =>
        {
            var result = await service.DeleteAsync(id);

            return result.ToHttpResult(() => Results.NoContent());
        }).RequireAuthorization("Admin");

        // PATCH /api/menu/{id}/stop — переключить стоп-лист
        group.MapPatch("/{id:int}/stop", async (
            int id,
            [FromBody] StopProductDto dto,
            ProductService service) =>
        {
            var result = await service.ToggleStopAsync(id, dto.IsStopped);

            return result.ToHttpResult(() => Results.Ok(new
            {
                id,
                isStopped = dto.IsStopped,
                message = dto.IsStopped ? "Product stopped" : "Product resumed"
            }));
        }).RequireAuthorization("AdminOrCurator");

        // POST /api/menu/validate — проверить существование товаров
        group.MapPost("/validate", async (
            [FromBody] List<Guid> productIds,
            ProductService service) =>
        {
            // TODO: реализовать, когда появится соответствующий метод в ProductService
            // Пример:
            // var result = await service.ValidateProductsAsync(productIds);
            // return result.ToHttpResult();
            app;
            return Results.Ok();
        });

        // POST /api/menu/details — получить детали товаров по списку ID
        group.MapPost("/details", async (
            [FromBody] List<int> productIds,
            ProductService service) =>
        {
            // TODO: реализовать, когда появится соответствующий метод в ProductService
            // Пример:
            // var result = await service.GetDetailsAsync(productIds);
            // return result.ToHttpResult();
            return Results.Ok();
        });

        return app;
    }
}
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
            ProductService service,
            HttpContext context) =>
        {
            try
            {
                var result = await service.GetMenuAsync(queryParams);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // GET /api/menu/{id} — получить товар по ID
        group.MapGet("/{id:int}", async (
            int id,
            ProductService service) =>
        {
            try
            {
                var product = await service.GetByIdAsync(id);
                return Results.Ok(product);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new { error = $"Product with ID {id} not found" });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/menu — создать новый товар
        group.MapPost("/", async (
            CreateProductDto dto,
            ProductService service,
            HttpContext context) =>
        {
            try
            {
                var product = await service.CreateAsync(dto);
                return Results.Created($"/api/menu/{product.Id}", product);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }).RequireAuthorization("Admin");

        // PATCH /api/menu/{id} — обновить товар
        group.MapPatch("/{id:int}", async (
            int id,
            UpdateProductDto dto,
            ProductService service) =>
        {
            try
            {
                var product = await service.UpdateAsync(id, dto);
                return Results.Ok(product);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new { error = $"Product with ID {id} not found" });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }).RequireAuthorization("AdminOrCurator");

        // DELETE /api/menu/{id} — удалить товар
        group.MapDelete("/{id:int}", async (
            int id,
            ProductService service) =>
        {
            try
            {
                await service.DeleteAsync(id);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new { error = $"Product with ID {id} not found" });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }).RequireAuthorization("Admin");

        // PATCH /api/menu/{id}/stop — переключить стоп-лист
        group.MapPatch("/{id:int}/stop", async (
            int id,
            [FromBody] StopProductDto dto,
            ProductService service) =>
        {
            try
            {
                await service.ToggleStopAsync(id, dto.IsStopped);
                return Results.Ok(new
                    { id, isStopped = dto.IsStopped, message = dto.IsStopped ? "Product stopped" : "Product resumed" });
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new { error = $"Product with ID {id} not found" });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }).RequireAuthorization("AdminOrCurator");
        
        return app;
    }
}
namespace OrderServices;

public static class Endpoints
{
    public static WebApplication BuildWebApplication(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders");
        group.MapGet("/my", () =>
        {

        });
        group.MapPost("/create", () =>
        {

        });
        group.MapGet("/{id}", () =>
        {

        });

        group.MapPatch("/{id}/cancel", () =>
        {

        });

        group.MapPost("/{id}/payment-confirm", () =>
        {

        });

        group.MapGet("/{id}/status", () =>
        {

        });
        
        return app;
    }
}
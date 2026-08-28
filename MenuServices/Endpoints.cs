namespace MenuServices;

public static class Endpoints
{
    public static WebApplication AddEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api");
        group.MapGet("/menu",
            async (HttpContext context) =>
            {
                //just get all menu for u restoran
            });

        group.MapPost("/menu", (HttpContext context) =>
        {
            // try to add new product in menu
        }).RequireAuthorization("UserIsAdmin");

        group.MapPatch("/menu/{id}",
            (HttpContext context) =>
            {
                //try to add in stop list product if u can do this
            }).RequireAuthorization("UserIsAdminOrCurator");

        group.MapGet("/menu/{id}", (HttpContext context) =>
            {
                //get all of this product
            }
        );

        return app;
    }
}
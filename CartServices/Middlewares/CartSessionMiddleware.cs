namespace CartServices.Middlewares;

public class CartSessionMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        
        var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;

        
        if (isAuthenticated)
        {
            
            await next(context);
            return;
        }

        // ✅ Для анонимных — создаем сессию
        if (!context.Request.Cookies.TryGetValue("cart_session", out var sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
            context.Response.Cookies.Append("cart_session", sessionId, new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(30),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps
            });
        }

        context.Items["CartSessionId"] = sessionId;
        await next(context);
    }
}
using AuthorizationMicroservices;
using AuthorizationMicroservices.Dto;
using Microsoft.AspNetCore.Identity.Data;

public static class Endpoints
{
    public static WebApplication AddEndpoints(this WebApplication app)
    {
        app.MapPost("/register", async (
            RegisterDto request,
            UserService userService) =>
        {
            var name = request.Name;
            userService.AddUser(request.Name, request.Email, request.Password);
            return true ? Results.Ok(new { Message = "User registered successfully" }) 
                           : Results.BadRequest("User already exists");
        });

        app.MapPost("/login", async (
            LoginRequest request,
            UserService userService,
            TokenService tokenService,
            HttpContext context) =>
        {
            var success= userService.Login(request.Email, request.Password);
            if (!success)
                return Results.BadRequest("Invalid email or password");
            var userid = userService.GetUserId(request.Email) ?? Guid.Empty;
            // 1. Генерируем Access Token
            var accessToken = tokenService.GenerateAccessToken(userid);

            // 2. Генерируем Refresh Token
            var refreshToken = tokenService.GenerateRefreshToken();
            tokenService.StoreRefreshToken(refreshToken, userid, TimeSpan.FromDays(7));

            // 3. Кладем Refresh Token в HttpOnly Cookie
            context.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            // 4. Возвращаем Access Token в теле ответа
            return Results.Ok(accessToken);
        });

        app.MapPost("/refresh", async (
            HttpContext context,
            TokenService tokenService) =>
        {
            // 1. Забираем Refresh Token из куки
            var refreshToken = context.Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(refreshToken))
                return Results.Unauthorized();

            // 2. Валидируем Refresh Token
            if (!tokenService.ValidateRefreshToken(refreshToken, out Guid userId))
                return Results.Unauthorized();

            // 3. Генерируем новый Access Token
            var newAccessToken = tokenService.GenerateAccessToken(userId);

            // 4. Генерируем новый Refresh Token (для безопасности — циклическое обновление)
            var newRefreshToken = tokenService.GenerateRefreshToken();
            tokenService.StoreRefreshToken(newRefreshToken, userId, TimeSpan.FromDays(7));
            tokenService.RevokeRefreshToken(refreshToken); // Удаляем старый

            // 5. Обновляем куку
            context.Response.Cookies.Delete("refresh_token");
            context.Response.Cookies.Append("refresh_token", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            // 6. Возвращаем новый Access Token
            return Results.Ok(newAccessToken);
        });

        app.MapPost("/logout", async (
            HttpContext context,
            TokenService tokenService) =>
        {
            // 1. Забираем Refresh Token из куки
            var refreshToken = context.Request.Cookies["refresh_token"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                tokenService.RevokeRefreshToken(refreshToken);
            }

            // 2. Удаляем куку
            context.Response.Cookies.Delete("refresh_token");

            return Results.Ok(new { Message = "Logged out successfully" });
        });

        return app;
    }
}
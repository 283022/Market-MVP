using AuthorizationMicroservices.Dto;
using Microsoft.AspNetCore.Identity.Data;

namespace AuthorizationMicroservices;

public static class Endpoints
{
    public static WebApplication AddEndpoints(this WebApplication app)
    {
        app.MapPost("/register", (
            RegisterDto request,
            UserService userService) =>
        {
            var result = userService.AddUser(request.Name, request.Email, request.Password);

            return result.ToHttpResult(userId =>
                Results.Ok(new
                {
                    Message = "User registered successfully",
                    UserId = userId
                }));
        });

        app.MapPost("/login", (
            LoginRequest request,
            UserService userService,
            TokenService tokenService,
            HttpContext context) =>
        {
            var loginResult = userService.Login(request.Email, request.Password);
            if (loginResult.IsFailed)
                return loginResult.ToHttpResult();

            var userId = loginResult.Value;

            // 1. Генерируем Access Token
            var accessToken = tokenService.GenerateAccessToken(userId);

            // 2. Генерируем Refresh Token
            var refreshToken = tokenService.GenerateRefreshToken();
            tokenService.StoreRefreshToken(refreshToken, userId, TimeSpan.FromDays(7));

            // 3. Кладём Refresh Token в HttpOnly Cookie
            context.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            // 4. Возвращаем Access Token в теле ответа
            return Results.Ok(new { AccessToken = accessToken });
        });

        app.MapPost("/refresh", (
            HttpContext context,
            TokenService tokenService) =>
        {
            // 1. Забираем Refresh Token из куки
            var refreshToken = context.Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(refreshToken))
                return Results.Unauthorized();

            // 2. Валидируем Refresh Token
            if (!tokenService.ValidateRefreshToken(refreshToken, out var userId))
                return Results.Unauthorized();

            // 3. Генерируем новый Access Token
            var newAccessToken = tokenService.GenerateAccessToken(userId);

            // 4. Генерируем новый Refresh Token (циклическое обновление)
            var newRefreshToken = tokenService.GenerateRefreshToken();
            tokenService.StoreRefreshToken(newRefreshToken, userId, TimeSpan.FromDays(7));
            tokenService.RevokeRefreshToken(refreshToken);

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
            return Results.Ok(new { AccessToken = newAccessToken });
        });

        app.MapPost("/logout", (
            HttpContext context,
            TokenService tokenService) =>
        {
            var refreshToken = context.Request.Cookies["refresh_token"];
            if (!string.IsNullOrEmpty(refreshToken))
                tokenService.RevokeRefreshToken(refreshToken);

            context.Response.Cookies.Delete("refresh_token");

            return Results.Ok(new { Message = "Logged out successfully" });
        });

        return app;
    }
}
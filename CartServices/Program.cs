using System.Text;
using CartServices;
using CartServices.Clients;
using CartServices.Middlewares;
using CartServices.Repository;
using CartServices.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// JWT настройки
var jwtSecret = builder.Configuration["Jwt:Secret"] 
                ?? throw new InvalidOperationException("JWT Secret is not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret) // 
            )
        };
    });

//Авторизация
builder.Services.AddAuthorization();

//HTTP клиенты
builder.Services.AddHttpClient<MenuClient>()
    .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(new[]
    {
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(1)
    }));

// DI
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<UnitOfWork>();
builder.Services.AddScoped<CartRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

//JWT проверка
app.UseAuthentication();
app.UseMiddleware<CartSessionMiddleware>();
app.UseAuthorization();

app.AddEndpoints();

app.Run();
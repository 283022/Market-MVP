using System.Text;
using MenuServices;
using MenuServices.Db;
using MenuServices.Repository;
using MenuServices.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<UnitOfWorkEfCore>();
builder.Services.AddScoped<ProductService>();

var secret = builder.Configuration["AppSettings:Token"] 
    ?? throw new InvalidOperationException("JWT Token is not configured");
var issuer = builder.Configuration["JwtIssuer"];
var audience = builder.Configuration["JwtAudience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,   // ← Проверка exp!
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ClockSkew = TimeSpan.FromSeconds(5)
        };
    });

// === Авторизация (политики) ===
builder.Services.AddAuthorization(options =>
{
    //Простая роль
    options.AddPolicy("UserIsAdmin", policy =>
        policy.RequireRole("Admin"));

    //Админ или Куратор
    options.AddPolicy("UserIsAdminOrCurator", policy =>
        policy.RequireRole("Admin", "Curator"));

    //Только аутентифицированный пользователь
    options.AddPolicy("AuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();  // Проверяет JWT и создает User
app.UseAuthorization();   // Проверяет политики

app.AddEndpoints();

app.Run();
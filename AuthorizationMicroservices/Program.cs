using System.Text;
using AuthorizationMicroservices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);


var secret = builder.Configuration.GetSection("AppSettings:Token").Value ?? throw new InvalidOperationException("Missing configuration Token");
var issuer = builder.Configuration["JwtIssuer"]
              ?? throw new InvalidOperationException(
                  "JwtIssuer is not configured");

var audience = builder.Configuration["JwtAudience"]
                ?? throw new InvalidOperationException(
                    "JwtAudience is not configured");


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ClockSkew = TimeSpan.FromSeconds(5)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddMemoryCache(); 
builder.Services.AddSingleton<UserService>();   
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<Hasher>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 4. Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStatusCodePages();
app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication(); 
app.UseAuthorization();


app.AddEndpoints(); 


app.Run();
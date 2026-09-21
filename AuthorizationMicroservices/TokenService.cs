using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace AuthorizationMicroservices;

public class TokenService
{
    private readonly SymmetricSecurityKey _securityKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly IMemoryCache _refreshTokenCache; // Или Redis

    public TokenService(IConfiguration config, IMemoryCache cache)
    {
        var secret = config.GetSection("AppSettings:Token").Value;
        if (string.IsNullOrEmpty(secret))
            throw new InvalidOperationException("AppSettings:Token is not configured");
        _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        
        _issuer = config["JwtIssuer"];
        _audience = config["JwtAudience"];
        if (string.IsNullOrEmpty(_audience) ||  string.IsNullOrEmpty(_issuer))
            throw new InvalidOperationException("AppSettings:Token is not configured");

        _refreshTokenCache = cache;
    }

    public string GenerateAccessToken(Guid userId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ]),
            Expires = DateTime.UtcNow.AddMinutes(15),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        return refreshToken;
    }

    //TODO: когда реализация будет через бд, важно чтобы валидировалось время рефреш токена
    public bool ValidateRefreshToken(string refreshToken, out Guid userId)
    {
        userId = Guid.Empty;

        // Проверяем, есть ли токен в кеше
        if (!_refreshTokenCache.TryGetValue(refreshToken, out Guid cachedUserId))
            return false;
        userId = cachedUserId;
        return true;
    }

    public void StoreRefreshToken(string refreshToken, Guid userId, TimeSpan expiry)
    {
        _refreshTokenCache.Set(refreshToken, userId,expiry);
    }

    public void RevokeRefreshToken(string refreshToken)
    {
        _refreshTokenCache.Remove(refreshToken);
    }
}
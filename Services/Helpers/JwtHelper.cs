using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HiveCore.Models.Entities;
using HiveCore.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HiveCore.Services.Helpers;

public interface IJwtHelper
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token, bool validateLifetime = true);
    DateTime GetAccessTokenExpiry();
}

public class JwtHelper : IJwtHelper
{
    private readonly IConfiguration _config;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessMinutes;
    private readonly int _refreshDays;

    public JwtHelper(IConfiguration config)
    {
        _config = config;

        var key = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        if (key.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        _issuer = _config["Jwt:Issuer"] ?? "https://hivecore.app";
        _audience = _config["Jwt:Audience"] ?? "https://hivecore.app";
        _accessMinutes = int.TryParse(_config["Jwt:AccessTokenExpirationMinutes"], out var m) ? m : 60;
        _refreshDays = int.TryParse(_config["Jwt:RefreshTokenExpirationDays"], out var d) ? d : 30;
    }

    public string GenerateAccessToken(User user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateHelper.ToUnixSeconds(DateTime.UtcNow).ToString(),
                ClaimValueTypes.Integer64),
            new(Constants.Claims.UserId, user.Id.ToString()),
            new(Constants.Claims.Username, user.UserName ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(Constants.Claims.IsVerified, user.IsVerified.ToString().ToLowerInvariant())
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var creds = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: GetAccessTokenExpiry(),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? ValidateToken(string token, bool validateLifetime = true)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = validateLifetime,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        try
        {
            var principal = handler.ValidateToken(token, parameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public DateTime GetAccessTokenExpiry() => DateTime.UtcNow.AddMinutes(_accessMinutes);
}
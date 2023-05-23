namespace ActiverWebAPI.Services.Middlewares;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ActiverWebAPI.Enums;
using ActiverWebAPI.Models.DBEntity;
using ActiverWebAPI.Models.DTO;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NuGet.Common;

public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public TokenDTO GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, ((UserRole) user.UserRole).ToString())
        };

        var expiresIn = 7;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresDate = DateTime.UtcNow.AddDays(expiresIn);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiresDate,
            signingCredentials: creds
        );

        return new TokenDTO
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpireAt = expiresDate
        };   
    }
}

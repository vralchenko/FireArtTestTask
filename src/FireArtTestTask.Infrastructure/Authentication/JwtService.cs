using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FireArtTestTask.Application.Abstractions;
using FireArtTestTask.Domain.Entities;
using FireArtTestTask.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FireArtTestTask.Infrastructure.Authentication;

public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;

    public JwtService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

using System.IdentityModel.Tokens.Jwt;
using FireArtTestTask.Domain.Entities;
using FireArtTestTask.Infrastructure.Authentication;
using FireArtTestTask.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace FireArtTestTask.Tests.Unit.Services;

public class JwtServiceTests
{
    private readonly JwtService _sut;

    public JwtServiceTests()
    {
        var settings = Options.Create(new JwtSettings
        {
            Secret = "ThisIsATestSecretKeyThatIsLongEnoughForHmacSha256!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationInMinutes = 60
        });
        _sut = new JwtService(settings);
    }

    [Fact]
    public void GenerateToken_ReturnsValidJwt()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com" };

        var token = _sut.GenerateToken(user);

        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_ContainsCorrectClaims()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };

        var token = _sut.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Value == userId.ToString());
        jwt.Claims.Should().Contain(c => c.Value == "test@example.com");
    }

    [Fact]
    public void GenerateToken_HasCorrectExpiration()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com" };

        var token = _sut.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateToken_HasCorrectIssuer()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com" };

        var token = _sut.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        jwt.Issuer.Should().Be("TestIssuer");
    }

    [Fact]
    public void GenerateToken_HasCorrectAudience()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com" };

        var token = _sut.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        jwt.Audiences.Should().Contain("TestAudience");
    }

    [Fact]
    public void GenerateToken_DifferentUsers_GetDifferentTokens()
    {
        var user1 = new User { Id = Guid.NewGuid(), Email = "user1@example.com" };
        var user2 = new User { Id = Guid.NewGuid(), Email = "user2@example.com" };

        var token1 = _sut.GenerateToken(user1);
        var token2 = _sut.GenerateToken(user2);

        token1.Should().NotBe(token2);
    }
}

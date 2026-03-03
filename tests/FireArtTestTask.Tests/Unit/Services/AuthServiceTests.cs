using FireArtTestTask.Api.Data;
using FireArtTestTask.Api.DTOs.Auth;
using FireArtTestTask.Api.Entities;
using FireArtTestTask.Api.Exceptions;
using FireArtTestTask.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FireArtTestTask.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AuthTest_" + Guid.NewGuid())
            .Options;
        _db = new AppDbContext(options);
        _jwtServiceMock = new Mock<IJwtService>();
        _emailServiceMock = new Mock<IEmailService>();
        _jwtServiceMock.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("test-token");
        _sut = new AuthService(_db, _jwtServiceMock.Object, _emailServiceMock.Object);
    }

    [Fact]
    public async Task Signup_NewUser_ReturnsAuthResponse()
    {
        var request = new SignupRequest("new@example.com", "password123");

        var result = await _sut.SignupAsync(request);

        result.Token.Should().Be("test-token");
        result.Email.Should().Be("new@example.com");
        (await _db.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Signup_DuplicateEmail_ThrowsConflict()
    {
        _db.Users.Add(new User { Id = Guid.NewGuid(), Email = "exists@example.com", PasswordHash = "hash" });
        await _db.SaveChangesAsync();

        var act = () => _sut.SignupAsync(new SignupRequest("exists@example.com", "password"));

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Signup_HashesPassword()
    {
        await _sut.SignupAsync(new SignupRequest("hash@example.com", "password123"));

        var user = await _db.Users.FirstAsync();
        user.PasswordHash.Should().NotBe("password123");
        BCrypt.Net.BCrypt.Verify("password123", user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        await _sut.SignupAsync(new SignupRequest("login@example.com", "password123"));

        var result = await _sut.LoginAsync(new LoginRequest("login@example.com", "password123"));

        result.Token.Should().Be("test-token");
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        await _sut.SignupAsync(new SignupRequest("login2@example.com", "password123"));

        var act = () => _sut.LoginAsync(new LoginRequest("login2@example.com", "wrong"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Login_NonExistentUser_ThrowsUnauthorized()
    {
        var act = () => _sut.LoginAsync(new LoginRequest("no@example.com", "password"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ForgotPassword_ExistingUser_SetsResetToken()
    {
        await _sut.SignupAsync(new SignupRequest("forgot@example.com", "password123"));

        await _sut.ForgotPasswordAsync(new ForgotPasswordRequest("forgot@example.com"));

        var user = await _db.Users.FirstAsync(u => u.Email == "forgot@example.com");
        user.ResetToken.Should().NotBeNullOrEmpty();
        user.ResetTokenExpiry.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ForgotPassword_NonExistentUser_DoesNotThrow()
    {
        var act = () => _sut.ForgotPasswordAsync(new ForgotPasswordRequest("no@example.com"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ResetPassword_ValidToken_ChangesPassword()
    {
        await _sut.SignupAsync(new SignupRequest("reset@example.com", "old-password"));
        await _sut.ForgotPasswordAsync(new ForgotPasswordRequest("reset@example.com"));
        var user = await _db.Users.FirstAsync(u => u.Email == "reset@example.com");
        var token = user.ResetToken!;

        await _sut.ResetPasswordAsync(new ResetPasswordRequest("reset@example.com", token, "new-password"));

        var updatedUser = await _db.Users.FirstAsync(u => u.Email == "reset@example.com");
        BCrypt.Net.BCrypt.Verify("new-password", updatedUser.PasswordHash).Should().BeTrue();
        updatedUser.ResetToken.Should().BeNull();
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ThrowsUnauthorized()
    {
        await _sut.SignupAsync(new SignupRequest("reset2@example.com", "password"));
        await _sut.ForgotPasswordAsync(new ForgotPasswordRequest("reset2@example.com"));

        var act = () => _sut.ResetPasswordAsync(
            new ResetPasswordRequest("reset2@example.com", "wrong-token", "new-password"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}

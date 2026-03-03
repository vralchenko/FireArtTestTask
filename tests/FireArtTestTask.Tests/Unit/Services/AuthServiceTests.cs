using FireArtTestTask.Application.Abstractions;
using FireArtTestTask.Application.Auth.Commands;
using FireArtTestTask.Application.Exceptions;
using FireArtTestTask.Domain.Entities;
using FireArtTestTask.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FireArtTestTask.Tests.Unit.Services;

public class AuthHandlerTests
{
    private readonly AppDbContext _db;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;

    public AuthHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AuthTest_" + Guid.NewGuid())
            .Options;
        _db = new AppDbContext(options);
        _jwtServiceMock = new Mock<IJwtService>();
        _emailServiceMock = new Mock<IEmailService>();
        _jwtServiceMock.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("test-token");
    }

    [Fact]
    public async Task Signup_NewUser_ReturnsAuthResponse()
    {
        var handler = new SignupCommandHandler(_db, _jwtServiceMock.Object);
        var command = new SignupCommand("new@example.com", "password123");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Token.Should().Be("test-token");
        result.Email.Should().Be("new@example.com");
        (await _db.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Signup_DuplicateEmail_ThrowsConflict()
    {
        _db.Users.Add(new User { Id = Guid.NewGuid(), Email = "exists@example.com", PasswordHash = "hash" });
        await _db.SaveChangesAsync();

        var handler = new SignupCommandHandler(_db, _jwtServiceMock.Object);
        var act = () => handler.Handle(new SignupCommand("exists@example.com", "password"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Signup_HashesPassword()
    {
        var handler = new SignupCommandHandler(_db, _jwtServiceMock.Object);
        await handler.Handle(new SignupCommand("hash@example.com", "password123"), CancellationToken.None);

        var user = await _db.Users.FirstAsync();
        user.PasswordHash.Should().NotBe("password123");
        BCrypt.Net.BCrypt.Verify("password123", user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var signupHandler = new SignupCommandHandler(_db, _jwtServiceMock.Object);
        await signupHandler.Handle(new SignupCommand("login@example.com", "password123"), CancellationToken.None);

        var loginHandler = new LoginCommandHandler(_db, _jwtServiceMock.Object);
        var result = await loginHandler.Handle(new LoginCommand("login@example.com", "password123"), CancellationToken.None);

        result.Token.Should().Be("test-token");
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        var signupHandler = new SignupCommandHandler(_db, _jwtServiceMock.Object);
        await signupHandler.Handle(new SignupCommand("login2@example.com", "password123"), CancellationToken.None);

        var loginHandler = new LoginCommandHandler(_db, _jwtServiceMock.Object);
        var act = () => loginHandler.Handle(new LoginCommand("login2@example.com", "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Login_NonExistentUser_ThrowsUnauthorized()
    {
        var loginHandler = new LoginCommandHandler(_db, _jwtServiceMock.Object);
        var act = () => loginHandler.Handle(new LoginCommand("no@example.com", "password"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ForgotPassword_ExistingUser_SetsResetToken()
    {
        var signupHandler = new SignupCommandHandler(_db, _jwtServiceMock.Object);
        await signupHandler.Handle(new SignupCommand("forgot@example.com", "password123"), CancellationToken.None);

        var forgotHandler = new ForgotPasswordCommandHandler(_db, _emailServiceMock.Object);
        await forgotHandler.Handle(new ForgotPasswordCommand("forgot@example.com"), CancellationToken.None);

        var user = await _db.Users.FirstAsync(u => u.Email == "forgot@example.com");
        user.ResetToken.Should().NotBeNullOrEmpty();
        user.ResetTokenExpiry.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ForgotPassword_NonExistentUser_DoesNotThrow()
    {
        var forgotHandler = new ForgotPasswordCommandHandler(_db, _emailServiceMock.Object);
        var act = () => forgotHandler.Handle(new ForgotPasswordCommand("no@example.com"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ResetPassword_ValidToken_ChangesPassword()
    {
        var signupHandler = new SignupCommandHandler(_db, _jwtServiceMock.Object);
        await signupHandler.Handle(new SignupCommand("reset@example.com", "old-password"), CancellationToken.None);

        var forgotHandler = new ForgotPasswordCommandHandler(_db, _emailServiceMock.Object);
        await forgotHandler.Handle(new ForgotPasswordCommand("reset@example.com"), CancellationToken.None);
        var user = await _db.Users.FirstAsync(u => u.Email == "reset@example.com");
        var token = user.ResetToken!;

        var resetHandler = new ResetPasswordCommandHandler(_db);
        await resetHandler.Handle(new ResetPasswordCommand("reset@example.com", token, "new-password"), CancellationToken.None);

        var updatedUser = await _db.Users.FirstAsync(u => u.Email == "reset@example.com");
        BCrypt.Net.BCrypt.Verify("new-password", updatedUser.PasswordHash).Should().BeTrue();
        updatedUser.ResetToken.Should().BeNull();
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ThrowsUnauthorized()
    {
        var signupHandler = new SignupCommandHandler(_db, _jwtServiceMock.Object);
        await signupHandler.Handle(new SignupCommand("reset2@example.com", "password"), CancellationToken.None);

        var forgotHandler = new ForgotPasswordCommandHandler(_db, _emailServiceMock.Object);
        await forgotHandler.Handle(new ForgotPasswordCommand("reset2@example.com"), CancellationToken.None);

        var resetHandler = new ResetPasswordCommandHandler(_db);
        var act = () => resetHandler.Handle(
            new ResetPasswordCommand("reset2@example.com", "wrong-token", "new-password"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}

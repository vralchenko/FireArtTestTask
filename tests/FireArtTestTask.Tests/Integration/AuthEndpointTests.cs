using System.Net;
using System.Net.Http.Json;
using FireArtTestTask.Application.Auth.Commands;
using FireArtTestTask.Application.DTOs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FireArtTestTask.Tests.Integration;

public class AuthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ===================================================================
    // Signup — happy path
    // ===================================================================

    [Fact]
    public async Task Signup_ValidRequest_ReturnsToken()
    {
        var request = new SignupCommand("test@example.com", "password123");

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result!.Token.Should().NotBeNullOrEmpty();
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Signup_DuplicateEmail_ReturnsConflict()
    {
        var request = new SignupCommand("duplicate@example.com", "password123");
        await _client.PostAsJsonAsync("/api/auth/signup", request);

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Signup_InvalidEmail_ReturnsBadRequest()
    {
        var request = new SignupCommand("not-an-email", "password123");

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ===================================================================
    // Signup — edge cases
    // ===================================================================

    [Fact]
    public async Task Signup_EmptyBody_ReturnsBadRequest()
    {
        // record defaults: Email = "", Password = ""
        var request = new SignupCommand("", "");

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Signup_PasswordExactlySixChars_Succeeds()
    {
        var email = $"sixchar-{Guid.NewGuid()}@example.com";
        var request = new SignupCommand(email, "abcdef"); // exactly 6 chars

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result!.Token.Should().NotBeNullOrEmpty();
        result.Email.Should().Be(email);
    }

    [Fact]
    public async Task Signup_PasswordFiveChars_ReturnsBadRequest()
    {
        var email = $"fivechar-{Guid.NewGuid()}@example.com";
        var request = new SignupCommand(email, "abcde"); // only 5 chars

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Signup_VeryLongEmail_DoesNotCrashServer()
    {
        // Email > 256 chars — InMemory DB does not enforce max length constraints,
        // and FluentValidation's EmailAddress() considers it a valid format.
        // The important thing is the server doesn't 500.
        var longLocal = new string('a', 250);
        var email = $"{longLocal}@example.com"; // 262 chars
        var request = new SignupCommand(email, "password123");

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        // Either validation rejects it (400) or it goes through (200) — not a 500
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Signup_SqlInjectionInEmail_ReturnsBadRequest()
    {
        var request = new SignupCommand("' OR 1=1 --", "password123");

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        // Invalid email format -> validation rejects it
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Signup_ResponseContainsBothTokenAndEmail()
    {
        var email = $"fields-check-{Guid.NewGuid()}@example.com";
        var request = new SignupCommand(email, "password123");

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.Email.Should().NotBeNullOrEmpty();
        result.Email.Should().Be(email);
    }

    // ===================================================================
    // Login — happy path
    // ===================================================================

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var signup = new SignupCommand("login-test@example.com", "password123");
        await _client.PostAsJsonAsync("/api/auth/signup", signup);

        var login = new LoginCommand("login-test@example.com", "password123");
        var response = await _client.PostAsJsonAsync("/api/auth/login", login);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var signup = new SignupCommand("wrong-pw@example.com", "password123");
        await _client.PostAsJsonAsync("/api/auth/signup", signup);

        var login = new LoginCommand("wrong-pw@example.com", "wrongpassword");
        var response = await _client.PostAsJsonAsync("/api/auth/login", login);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonExistentUser_ReturnsUnauthorized()
    {
        var login = new LoginCommand("nonexistent@example.com", "password123");

        var response = await _client.PostAsJsonAsync("/api/auth/login", login);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ===================================================================
    // Login — edge cases
    // ===================================================================

    [Fact]
    public async Task Login_EmptyBody_ReturnsBadRequest()
    {
        var request = new LoginCommand("", "");

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_EmptyEmail_ReturnsBadRequest()
    {
        var request = new LoginCommand("", "password123");

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_EmptyPassword_ReturnsBadRequest()
    {
        var email = $"login-empty-pw-{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/signup", new SignupCommand(email, "password123"));

        var request = new LoginCommand(email, "");

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_InvalidEmailFormat_ReturnsBadRequest()
    {
        var request = new LoginCommand("not-an-email", "password123");

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ===================================================================
    // Forgot Password — happy path
    // ===================================================================

    [Fact]
    public async Task ForgotPassword_ValidEmail_ReturnsOk()
    {
        var signup = new SignupCommand("forgot@example.com", "password123");
        await _client.PostAsJsonAsync("/api/auth/signup", signup);

        var request = new ForgotPasswordCommand("forgot@example.com");
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ===================================================================
    // Forgot Password — edge cases
    // ===================================================================

    [Fact]
    public async Task ForgotPassword_NonExistentEmail_StillReturnsOk()
    {
        // No user enumeration — always 200
        var request = new ForgotPasswordCommand("nobody-here@example.com");

        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_EmptyEmail_ReturnsBadRequest()
    {
        var request = new ForgotPasswordCommand("");

        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmailFormat_ReturnsBadRequest()
    {
        var request = new ForgotPasswordCommand("not-an-email-format");

        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ===================================================================
    // Reset Password — edge cases
    // ===================================================================

    [Fact]
    public async Task ResetPassword_ValidTokenResetsPasswordAndLoginWorks()
    {
        // Full flow integration test: signup -> forgot-password -> read token from DB -> reset -> login
        var email = $"reset-flow-{Guid.NewGuid()}@example.com";
        var originalPassword = "original123";
        var newPassword = "newpass123";

        // 1. Sign up
        await _client.PostAsJsonAsync("/api/auth/signup", new SignupCommand(email, originalPassword));

        // 2. Request password reset
        await _client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordCommand(email));

        // 3. Read reset token directly from DB via service scope
        string resetToken;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<FireArtTestTask.Application.Abstractions.IAppDbContext>();
            var user = await db.Users.FirstAsync(u => u.Email == email);
            resetToken = user.ResetToken!;
            resetToken.Should().NotBeNullOrEmpty();
        }

        // 4. Reset password
        var resetResponse = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordCommand(email, resetToken, newPassword));
        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Login with NEW password works
        var loginNew = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginCommand(email, newPassword));
        loginNew.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResult = await loginNew.Content.ReadFromJsonAsync<AuthResponse>();
        authResult!.Token.Should().NotBeNullOrEmpty();

        // 6. Login with OLD password fails
        var loginOld = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginCommand(email, originalPassword));
        loginOld.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetPassword_TokenReuseAfterReset_ReturnsUnauthorized()
    {
        var email = $"token-reuse-{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/signup", new SignupCommand(email, "password123"));
        await _client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordCommand(email));

        // Read token from DB
        string resetToken;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<FireArtTestTask.Application.Abstractions.IAppDbContext>();
            var user = await db.Users.FirstAsync(u => u.Email == email);
            resetToken = user.ResetToken!;
        }

        // First reset succeeds
        var firstReset = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordCommand(email, resetToken, "newpass123"));
        firstReset.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second reset with same token fails (token was cleared)
        var secondReset = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordCommand(email, resetToken, "anotherpass"));
        secondReset.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetPassword_EmptyBody_ReturnsBadRequest()
    {
        var request = new ResetPasswordCommand("", "", "");

        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsUnauthorized()
    {
        var email = $"bad-token-{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/signup", new SignupCommand(email, "password123"));
        await _client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordCommand(email));

        var request = new ResetPasswordCommand(email, "definitely-not-the-right-token", "newpass123");

        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetPassword_ShortNewPassword_ReturnsBadRequest()
    {
        var email = $"reset-short-pw-{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/signup", new SignupCommand(email, "password123"));
        await _client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordCommand(email));

        // NewPassword is only 5 chars -> validation fails
        var request = new ResetPasswordCommand(email, "some-token", "12345");

        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

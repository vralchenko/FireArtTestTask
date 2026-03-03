using System.Net;
using System.Net.Http.Json;
using FireArtTestTask.Application.Auth.Commands;
using FireArtTestTask.Application.DTOs;
using FluentAssertions;

namespace FireArtTestTask.Tests.Integration;

public class AuthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

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

    [Fact]
    public async Task ForgotPassword_ValidEmail_ReturnsOk()
    {
        var signup = new SignupCommand("forgot@example.com", "password123");
        await _client.PostAsJsonAsync("/api/auth/signup", signup);

        var request = new ForgotPasswordCommand("forgot@example.com");
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

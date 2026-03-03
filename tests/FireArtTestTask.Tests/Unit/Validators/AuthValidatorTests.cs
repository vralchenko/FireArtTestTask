using FireArtTestTask.Api.DTOs.Auth;
using FireArtTestTask.Api.Validators;
using FluentAssertions;

namespace FireArtTestTask.Tests.Unit.Validators;

public class AuthValidatorTests
{
    private readonly SignupRequestValidator _signupValidator = new();
    private readonly LoginRequestValidator _loginValidator = new();
    private readonly ForgotPasswordRequestValidator _forgotPasswordValidator = new();
    private readonly ResetPasswordRequestValidator _resetPasswordValidator = new();

    [Fact]
    public void SignupValidator_ValidRequest_Passes()
    {
        var result = _signupValidator.Validate(new SignupRequest("test@example.com", "password123"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SignupValidator_EmptyEmail_Fails()
    {
        var result = _signupValidator.Validate(new SignupRequest("", "password123"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SignupValidator_InvalidEmail_Fails()
    {
        var result = _signupValidator.Validate(new SignupRequest("not-email", "password123"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SignupValidator_ShortPassword_Fails()
    {
        var result = _signupValidator.Validate(new SignupRequest("test@example.com", "12345"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SignupValidator_EmptyPassword_Fails()
    {
        var result = _signupValidator.Validate(new SignupRequest("test@example.com", ""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LoginValidator_ValidRequest_Passes()
    {
        var result = _loginValidator.Validate(new LoginRequest("test@example.com", "password"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LoginValidator_EmptyEmail_Fails()
    {
        var result = _loginValidator.Validate(new LoginRequest("", "password"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ForgotPasswordValidator_ValidEmail_Passes()
    {
        var result = _forgotPasswordValidator.Validate(new ForgotPasswordRequest("test@example.com"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ForgotPasswordValidator_InvalidEmail_Fails()
    {
        var result = _forgotPasswordValidator.Validate(new ForgotPasswordRequest("bad"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ResetPasswordValidator_ValidRequest_Passes()
    {
        var result = _resetPasswordValidator.Validate(
            new ResetPasswordRequest("test@example.com", "token123", "newpass123"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ResetPasswordValidator_EmptyToken_Fails()
    {
        var result = _resetPasswordValidator.Validate(
            new ResetPasswordRequest("test@example.com", "", "newpass123"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ResetPasswordValidator_ShortPassword_Fails()
    {
        var result = _resetPasswordValidator.Validate(
            new ResetPasswordRequest("test@example.com", "token", "12345"));
        result.IsValid.Should().BeFalse();
    }
}

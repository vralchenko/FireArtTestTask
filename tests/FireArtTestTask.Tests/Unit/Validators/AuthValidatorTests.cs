using FireArtTestTask.Application.Auth.Commands;
using FireArtTestTask.Application.Validators;
using FluentAssertions;

namespace FireArtTestTask.Tests.Unit.Validators;

public class AuthValidatorTests
{
    private readonly SignupCommandValidator _signupValidator = new();
    private readonly LoginCommandValidator _loginValidator = new();
    private readonly ForgotPasswordCommandValidator _forgotPasswordValidator = new();
    private readonly ResetPasswordCommandValidator _resetPasswordValidator = new();

    [Fact]
    public void SignupValidator_ValidRequest_Passes()
    {
        var result = _signupValidator.Validate(new SignupCommand("test@example.com", "password123"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SignupValidator_EmptyEmail_Fails()
    {
        var result = _signupValidator.Validate(new SignupCommand("", "password123"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SignupValidator_InvalidEmail_Fails()
    {
        var result = _signupValidator.Validate(new SignupCommand("not-email", "password123"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SignupValidator_ShortPassword_Fails()
    {
        var result = _signupValidator.Validate(new SignupCommand("test@example.com", "12345"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SignupValidator_EmptyPassword_Fails()
    {
        var result = _signupValidator.Validate(new SignupCommand("test@example.com", ""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LoginValidator_ValidRequest_Passes()
    {
        var result = _loginValidator.Validate(new LoginCommand("test@example.com", "password"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LoginValidator_EmptyEmail_Fails()
    {
        var result = _loginValidator.Validate(new LoginCommand("", "password"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ForgotPasswordValidator_ValidEmail_Passes()
    {
        var result = _forgotPasswordValidator.Validate(new ForgotPasswordCommand("test@example.com"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ForgotPasswordValidator_InvalidEmail_Fails()
    {
        var result = _forgotPasswordValidator.Validate(new ForgotPasswordCommand("bad"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ResetPasswordValidator_ValidRequest_Passes()
    {
        var result = _resetPasswordValidator.Validate(
            new ResetPasswordCommand("test@example.com", "token123", "newpass123"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ResetPasswordValidator_EmptyToken_Fails()
    {
        var result = _resetPasswordValidator.Validate(
            new ResetPasswordCommand("test@example.com", "", "newpass123"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ResetPasswordValidator_ShortPassword_Fails()
    {
        var result = _resetPasswordValidator.Validate(
            new ResetPasswordCommand("test@example.com", "token", "12345"));
        result.IsValid.Should().BeFalse();
    }
}

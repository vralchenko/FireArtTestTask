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

    // ── SignupCommandValidator ───────────────────────────────────────

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
    public void SignupValidator_PasswordExactly6Chars_Passes()
    {
        var result = _signupValidator.Validate(new SignupCommand("test@example.com", "abcdef"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SignupValidator_PasswordExactly5Chars_Fails()
    {
        var result = _signupValidator.Validate(new SignupCommand("test@example.com", "abcde"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SignupValidator_EmailWithPlusTag_Passes()
    {
        var result = _signupValidator.Validate(new SignupCommand("user+tag@example.com", "password123"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SignupValidator_EmptyEmailAndEmptyPassword_HasMultipleErrors()
    {
        var result = _signupValidator.Validate(new SignupCommand("", ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // ── LoginCommandValidator ───────────────────────────────────────

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
    public void LoginValidator_EmptyPassword_Fails()
    {
        var result = _loginValidator.Validate(new LoginCommand("test@example.com", ""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LoginValidator_InvalidEmailFormat_Fails()
    {
        var result = _loginValidator.Validate(new LoginCommand("not-an-email", "password"));
        result.IsValid.Should().BeFalse();
    }

    // ── ForgotPasswordCommandValidator ──────────────────────────────

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
    public void ForgotPasswordValidator_EmptyEmail_Fails()
    {
        var result = _forgotPasswordValidator.Validate(new ForgotPasswordCommand(""));
        result.IsValid.Should().BeFalse();
    }

    // ── ResetPasswordCommandValidator ───────────────────────────────

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

    [Fact]
    public void ResetPasswordValidator_EmptyEmail_Fails()
    {
        var result = _resetPasswordValidator.Validate(
            new ResetPasswordCommand("", "token123", "newpass123"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ResetPasswordValidator_EmptyNewPassword_Fails()
    {
        var result = _resetPasswordValidator.Validate(
            new ResetPasswordCommand("test@example.com", "token123", ""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ResetPasswordValidator_NewPasswordExactly6Chars_Passes()
    {
        var result = _resetPasswordValidator.Validate(
            new ResetPasswordCommand("test@example.com", "token123", "abcdef"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ResetPasswordValidator_NewPassword5Chars_Fails()
    {
        var result = _resetPasswordValidator.Validate(
            new ResetPasswordCommand("test@example.com", "token123", "abcde"));
        result.IsValid.Should().BeFalse();
    }
}

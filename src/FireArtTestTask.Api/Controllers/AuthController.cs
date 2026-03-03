using FireArtTestTask.Api.DTOs.Auth;
using FireArtTestTask.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FireArtTestTask.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<SignupRequest> _signupValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;

    public AuthController(
        IAuthService authService,
        IValidator<SignupRequest> signupValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator)
    {
        _authService = authService;
        _signupValidator = signupValidator;
        _loginValidator = loginValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request)
    {
        await _signupValidator.ValidateAndThrowAsync(request);
        var result = await _authService.SignupAsync(request);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        await _loginValidator.ValidateAndThrowAsync(request);
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _forgotPasswordValidator.ValidateAndThrowAsync(request);
        await _authService.ForgotPasswordAsync(request);
        return Ok(new { message = "If the email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _resetPasswordValidator.ValidateAndThrowAsync(request);
        await _authService.ResetPasswordAsync(request);
        return Ok(new { message = "Password has been reset successfully." });
    }
}

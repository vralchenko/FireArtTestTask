using FireArtTestTask.Application.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FireArtTestTask.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        await _mediator.Send(command);
        return Ok(new { message = "If the email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        await _mediator.Send(command);
        return Ok(new { message = "Password has been reset successfully." });
    }
}

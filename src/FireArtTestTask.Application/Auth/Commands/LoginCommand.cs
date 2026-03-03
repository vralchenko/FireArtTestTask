using FireArtTestTask.Application.Abstractions;
using FireArtTestTask.Application.DTOs;
using FireArtTestTask.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FireArtTestTask.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IAppDbContext _db;
    private readonly IJwtService _jwtService;

    public LoginCommandHandler(IAppDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        var token = _jwtService.GenerateToken(user);
        return new AuthResponse(token, user.Email);
    }
}

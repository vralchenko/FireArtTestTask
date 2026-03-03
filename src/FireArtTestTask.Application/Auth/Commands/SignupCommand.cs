using FireArtTestTask.Application.Abstractions;
using FireArtTestTask.Application.DTOs;
using FireArtTestTask.Application.Exceptions;
using FireArtTestTask.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FireArtTestTask.Application.Auth.Commands;

public record SignupCommand(string Email, string Password) : IRequest<AuthResponse>;

public class SignupCommandHandler : IRequestHandler<SignupCommand, AuthResponse>
{
    private readonly IAppDbContext _db;
    private readonly IJwtService _jwtService;

    public SignupCommandHandler(IAppDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> Handle(SignupCommand request, CancellationToken cancellationToken)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            throw new ConflictException("User with this email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var token = _jwtService.GenerateToken(user);
        return new AuthResponse(token, user.Email);
    }
}

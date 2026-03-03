using FireArtTestTask.Api.Data;
using FireArtTestTask.Api.DTOs.Auth;
using FireArtTestTask.Api.Entities;
using FireArtTestTask.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FireArtTestTask.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;

    public AuthService(AppDbContext db, IJwtService jwtService, IEmailService emailService)
    {
        _db = db;
        _jwtService = jwtService;
        _emailService = emailService;
    }

    public async Task<AuthResponse> SignupAsync(SignupRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            throw new ConflictException("User with this email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);
        return new AuthResponse(token, user.Email);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        var token = _jwtService.GenerateToken(user);
        return new AuthResponse(token, user.Email);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
            return; // Don't reveal whether user exists

        user.ResetToken = Guid.NewGuid().ToString("N");
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _db.SaveChangesAsync();

        await _emailService.SendPasswordResetEmailAsync(user.Email, user.ResetToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email)
            ?? throw new UnauthorizedException("Invalid reset request.");

        if (user.ResetToken != request.Token || user.ResetTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedException("Invalid or expired reset token.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.ResetToken = null;
        user.ResetTokenExpiry = null;
        await _db.SaveChangesAsync();
    }
}

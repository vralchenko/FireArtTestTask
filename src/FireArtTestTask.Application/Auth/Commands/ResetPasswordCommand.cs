using FireArtTestTask.Application.Abstractions;
using FireArtTestTask.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FireArtTestTask.Application.Auth.Commands;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<Unit>;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Unit>
{
    private readonly IAppDbContext _db;

    public ResetPasswordCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken)
            ?? throw new UnauthorizedException("Invalid reset request.");

        if (user.ResetToken != request.Token || user.ResetTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedException("Invalid or expired reset token.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.ResetToken = null;
        user.ResetTokenExpiry = null;
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

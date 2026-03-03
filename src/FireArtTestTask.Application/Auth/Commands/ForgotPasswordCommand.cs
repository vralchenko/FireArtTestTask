using FireArtTestTask.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FireArtTestTask.Application.Auth.Commands;

public record ForgotPasswordCommand(string Email) : IRequest<Unit>;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly IEmailService _emailService;

    public ForgotPasswordCommandHandler(IAppDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user == null)
            return Unit.Value; // Don't reveal whether user exists

        user.ResetToken = Guid.NewGuid().ToString("N");
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _db.SaveChangesAsync(cancellationToken);

        await _emailService.SendPasswordResetEmailAsync(user.Email, user.ResetToken);

        return Unit.Value;
    }
}

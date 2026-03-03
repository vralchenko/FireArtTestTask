using FireArtTestTask.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace FireArtTestTask.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetToken)
    {
        // Stub: log instead of sending real email
        _logger.LogInformation("Password reset email for {Email}. Token: {Token}", email, resetToken);
        return Task.CompletedTask;
    }
}

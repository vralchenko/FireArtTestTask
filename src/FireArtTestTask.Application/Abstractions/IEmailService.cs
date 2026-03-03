namespace FireArtTestTask.Application.Abstractions;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetToken);
}

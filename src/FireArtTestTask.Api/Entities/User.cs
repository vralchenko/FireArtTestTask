namespace FireArtTestTask.Api.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

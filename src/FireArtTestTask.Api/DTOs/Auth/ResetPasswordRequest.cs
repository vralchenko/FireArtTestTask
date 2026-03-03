namespace FireArtTestTask.Api.DTOs.Auth;

public record ResetPasswordRequest(string Email, string Token, string NewPassword);

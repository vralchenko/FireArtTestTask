using FireArtTestTask.Api.DTOs.Auth;

namespace FireArtTestTask.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> SignupAsync(SignupRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
}

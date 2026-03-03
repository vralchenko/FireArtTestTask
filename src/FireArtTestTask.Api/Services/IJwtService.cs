using FireArtTestTask.Api.Entities;

namespace FireArtTestTask.Api.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}

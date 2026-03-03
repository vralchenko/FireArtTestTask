using FireArtTestTask.Domain.Entities;

namespace FireArtTestTask.Application.Abstractions;

public interface IJwtService
{
    string GenerateToken(User user);
}

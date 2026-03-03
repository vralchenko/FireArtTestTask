namespace FireArtTestTask.Application.DTOs;

public record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Category,
    DateTime CreatedAt,
    DateTime UpdatedAt);

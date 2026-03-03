namespace FireArtTestTask.Api.DTOs.Products;

public record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Category,
    DateTime CreatedAt,
    DateTime UpdatedAt);

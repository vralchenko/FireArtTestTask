namespace FireArtTestTask.Api.DTOs.Products;

public record CreateProductRequest(string Name, string Description, decimal Price, string Category);

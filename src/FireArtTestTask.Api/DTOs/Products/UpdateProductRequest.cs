namespace FireArtTestTask.Api.DTOs.Products;

public record UpdateProductRequest(string Name, string Description, decimal Price, string Category);

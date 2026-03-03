using FireArtTestTask.Api.DTOs.Products;

namespace FireArtTestTask.Api.Services;

public interface IProductService
{
    Task<ProductResponse> CreateAsync(CreateProductRequest request);
    Task<ProductResponse> GetByIdAsync(Guid id);
    Task<PagedResponse<ProductResponse>> SearchAsync(ProductSearchRequest request);
    Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request);
    Task DeleteAsync(Guid id);
}

using FireArtTestTask.Api.Data;
using FireArtTestTask.Api.DTOs.Products;
using FireArtTestTask.Api.Entities;
using FireArtTestTask.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FireArtTestTask.Api.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return ToResponse(product);
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id)
    {
        var product = await _db.Products.FindAsync(id)
            ?? throw new NotFoundException($"Product with id '{id}' not found.");

        return ToResponse(product);
    }

    public async Task<PagedResponse<ProductResponse>> SearchAsync(ProductSearchRequest request)
    {
        var query = _db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                p.Description.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(p => p.Category == request.Category);

        if (request.MinPrice.HasValue)
            query = query.Where(p => p.Price >= request.MinPrice.Value);

        if (request.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= request.MaxPrice.Value);

        query = request.SortBy?.ToLower() switch
        {
            "price" => request.SortDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "category" => request.SortDescending ? query.OrderByDescending(p => p.Category) : query.OrderBy(p => p.Category),
            "createdat" => request.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            _ => request.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => ToResponse(p))
            .ToListAsync();

        return new PagedResponse<ProductResponse>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _db.Products.FindAsync(id)
            ?? throw new NotFoundException($"Product with id '{id}' not found.");

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Category = request.Category;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return ToResponse(product);
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _db.Products.FindAsync(id)
            ?? throw new NotFoundException($"Product with id '{id}' not found.");

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
    }

    private static ProductResponse ToResponse(Product p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.Category, p.CreatedAt, p.UpdatedAt);
}

using FireArtTestTask.Application.Abstractions;
using FireArtTestTask.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FireArtTestTask.Application.Products.Queries;

public class SearchProductsQuery : IRequest<PagedResponse<ProductResponse>>
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; } = "name";
    public bool SortDescending { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, PagedResponse<ProductResponse>>
{
    private readonly IAppDbContext _db;

    public SearchProductsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<ProductResponse>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
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

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductResponse(p.Id, p.Name, p.Description, p.Price, p.Category, p.CreatedAt, p.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<ProductResponse>(items, totalCount, request.Page, request.PageSize);
    }
}

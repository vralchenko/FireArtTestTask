using FireArtTestTask.Application.Abstractions;
using FireArtTestTask.Application.DTOs;
using FireArtTestTask.Application.Exceptions;
using MediatR;

namespace FireArtTestTask.Application.Products.Queries;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductResponse>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductResponse>
{
    private readonly IAppDbContext _db;

    public GetProductByIdQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<ProductResponse> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FindAsync(new object[] { request.Id }, cancellationToken)
            ?? throw new NotFoundException($"Product with id '{request.Id}' not found.");

        return new ProductResponse(product.Id, product.Name, product.Description, product.Price, product.Category, product.CreatedAt, product.UpdatedAt);
    }
}

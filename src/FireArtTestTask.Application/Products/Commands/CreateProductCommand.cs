using FireArtTestTask.Application.Abstractions;
using FireArtTestTask.Application.DTOs;
using FireArtTestTask.Domain.Entities;
using MediatR;

namespace FireArtTestTask.Application.Products.Commands;

public record CreateProductCommand(string Name, string Description, decimal Price, string Category) : IRequest<ProductResponse>;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductResponse>
{
    private readonly IAppDbContext _db;

    public CreateProductCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<ProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
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
        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(product);
    }

    private static ProductResponse ToResponse(Product p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.Category, p.CreatedAt, p.UpdatedAt);
}

using FireArtTestTask.Application.Abstractions;
using FireArtTestTask.Application.DTOs;
using FireArtTestTask.Application.Exceptions;
using MediatR;

namespace FireArtTestTask.Application.Products.Commands;

public record UpdateProductCommand(Guid Id, string Name, string Description, decimal Price, string Category) : IRequest<ProductResponse>;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductResponse>
{
    private readonly IAppDbContext _db;

    public UpdateProductCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<ProductResponse> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FindAsync(new object[] { request.Id }, cancellationToken)
            ?? throw new NotFoundException($"Product with id '{request.Id}' not found.");

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Category = request.Category;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new ProductResponse(product.Id, product.Name, product.Description, product.Price, product.Category, product.CreatedAt, product.UpdatedAt);
    }
}

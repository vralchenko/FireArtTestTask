using FireArtTestTask.Application.Abstractions;
using FireArtTestTask.Application.Exceptions;
using MediatR;

namespace FireArtTestTask.Application.Products.Commands;

public record DeleteProductCommand(Guid Id) : IRequest<Unit>;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Unit>
{
    private readonly IAppDbContext _db;

    public DeleteProductCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FindAsync(new object[] { request.Id }, cancellationToken)
            ?? throw new NotFoundException($"Product with id '{request.Id}' not found.");

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

using FireArtTestTask.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FireArtTestTask.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Product> Products { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

using FireArtTestTask.Application.Abstractions;
using FireArtTestTask.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FireArtTestTask.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
        });
    }
}

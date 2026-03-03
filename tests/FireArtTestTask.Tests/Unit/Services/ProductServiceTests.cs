using FireArtTestTask.Application.Exceptions;
using FireArtTestTask.Application.Products.Commands;
using FireArtTestTask.Application.Products.Queries;
using FireArtTestTask.Domain.Entities;
using FireArtTestTask.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FireArtTestTask.Tests.Unit.Services;

public class ProductHandlerTests
{
    private readonly AppDbContext _db;

    public ProductHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("ProductTest_" + Guid.NewGuid())
            .Options;
        _db = new AppDbContext(options);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsProduct()
    {
        var handler = new CreateProductCommandHandler(_db);
        var command = new CreateProductCommand("Widget", "A widget", 9.99m, "Gadgets");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Name.Should().Be("Widget");
        result.Price.Should().Be(9.99m);
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_SavesToDB()
    {
        var handler = new CreateProductCommandHandler(_db);
        await handler.Handle(new CreateProductCommand("Widget", "Desc", 10m, "Cat"), CancellationToken.None);

        (await _db.Products.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsProduct()
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "Test", Description = "D", Price = 5, Category = "C" };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var handler = new GetProductByIdQueryHandler(_db);
        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetById_NonExistent_ThrowsNotFound()
    {
        var handler = new GetProductByIdQueryHandler(_db);
        var act = () => handler.Handle(new GetProductByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Search_ByName_ReturnsMatches()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "Alpha", Description = "", Price = 10, Category = "A" },
            new Product { Id = Guid.NewGuid(), Name = "Beta", Description = "", Price = 20, Category = "B" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { Search = "Alpha" }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task Search_ByCategory_FiltersCorrectly()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "A", Description = "", Price = 10, Category = "Electronics" },
            new Product { Id = Guid.NewGuid(), Name = "B", Description = "", Price = 20, Category = "Books" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { Category = "Electronics" }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Category.Should().Be("Electronics");
    }

    [Fact]
    public async Task Search_PriceRange_FiltersCorrectly()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "Cheap", Description = "", Price = 5, Category = "C" },
            new Product { Id = Guid.NewGuid(), Name = "Expensive", Description = "", Price = 100, Category = "C" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { MinPrice = 10, MaxPrice = 50 }, CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_Pagination_Works()
    {
        for (int i = 0; i < 15; i++)
            _db.Products.Add(new Product { Id = Guid.NewGuid(), Name = $"Item{i:D2}", Description = "", Price = 1, Category = "C" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { Page = 2, PageSize = 10 }, CancellationToken.None);

        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Search_SortByPrice_Works()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "A", Description = "", Price = 30, Category = "C" },
            new Product { Id = Guid.NewGuid(), Name = "B", Description = "", Price = 10, Category = "C" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { SortBy = "price" }, CancellationToken.None);

        result.Items[0].Price.Should().BeLessThan(result.Items[1].Price);
    }

    [Fact]
    public async Task Update_Existing_ReturnsUpdated()
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "Old", Description = "Old", Price = 10, Category = "Old" };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var handler = new UpdateProductCommandHandler(_db);
        var result = await handler.Handle(new UpdateProductCommand(product.Id, "New", "New", 20, "New"), CancellationToken.None);

        result.Name.Should().Be("New");
        result.Price.Should().Be(20);
    }

    [Fact]
    public async Task Update_NonExistent_ThrowsNotFound()
    {
        var handler = new UpdateProductCommandHandler(_db);
        var act = () => handler.Handle(new UpdateProductCommand(Guid.NewGuid(), "N", "D", 1, "C"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Delete_Existing_RemovesFromDB()
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "Del", Description = "", Price = 1, Category = "C" };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var handler = new DeleteProductCommandHandler(_db);
        await handler.Handle(new DeleteProductCommand(product.Id), CancellationToken.None);

        (await _db.Products.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Delete_NonExistent_ThrowsNotFound()
    {
        var handler = new DeleteProductCommandHandler(_db);
        var act = () => handler.Handle(new DeleteProductCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

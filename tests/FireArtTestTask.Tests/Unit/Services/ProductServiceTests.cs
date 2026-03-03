using FireArtTestTask.Api.Data;
using FireArtTestTask.Api.DTOs.Products;
using FireArtTestTask.Api.Entities;
using FireArtTestTask.Api.Exceptions;
using FireArtTestTask.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FireArtTestTask.Tests.Unit.Services;

public class ProductServiceTests
{
    private readonly AppDbContext _db;
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("ProductTest_" + Guid.NewGuid())
            .Options;
        _db = new AppDbContext(options);
        _sut = new ProductService(_db);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsProduct()
    {
        var request = new CreateProductRequest("Widget", "A widget", 9.99m, "Gadgets");

        var result = await _sut.CreateAsync(request);

        result.Name.Should().Be("Widget");
        result.Price.Should().Be(9.99m);
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_SavesToDB()
    {
        await _sut.CreateAsync(new CreateProductRequest("Widget", "Desc", 10m, "Cat"));

        (await _db.Products.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsProduct()
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "Test", Description = "D", Price = 5, Category = "C" };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(product.Id);

        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetById_NonExistent_ThrowsNotFound()
    {
        var act = () => _sut.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Search_ByName_ReturnsMatches()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "Alpha", Description = "", Price = 10, Category = "A" },
            new Product { Id = Guid.NewGuid(), Name = "Beta", Description = "", Price = 20, Category = "B" });
        await _db.SaveChangesAsync();

        var result = await _sut.SearchAsync(new ProductSearchRequest { Search = "Alpha" });

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

        var result = await _sut.SearchAsync(new ProductSearchRequest { Category = "Electronics" });

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

        var result = await _sut.SearchAsync(new ProductSearchRequest { MinPrice = 10, MaxPrice = 50 });

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_Pagination_Works()
    {
        for (int i = 0; i < 15; i++)
            _db.Products.Add(new Product { Id = Guid.NewGuid(), Name = $"Item{i:D2}", Description = "", Price = 1, Category = "C" });
        await _db.SaveChangesAsync();

        var result = await _sut.SearchAsync(new ProductSearchRequest { Page = 2, PageSize = 10 });

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

        var result = await _sut.SearchAsync(new ProductSearchRequest { SortBy = "price" });

        result.Items[0].Price.Should().BeLessThan(result.Items[1].Price);
    }

    [Fact]
    public async Task Update_Existing_ReturnsUpdated()
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "Old", Description = "Old", Price = 10, Category = "Old" };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateAsync(product.Id, new UpdateProductRequest("New", "New", 20, "New"));

        result.Name.Should().Be("New");
        result.Price.Should().Be(20);
    }

    [Fact]
    public async Task Update_NonExistent_ThrowsNotFound()
    {
        var act = () => _sut.UpdateAsync(Guid.NewGuid(), new UpdateProductRequest("N", "D", 1, "C"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Delete_Existing_RemovesFromDB()
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "Del", Description = "", Price = 1, Category = "C" };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        await _sut.DeleteAsync(product.Id);

        (await _db.Products.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Delete_NonExistent_ThrowsNotFound()
    {
        var act = () => _sut.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

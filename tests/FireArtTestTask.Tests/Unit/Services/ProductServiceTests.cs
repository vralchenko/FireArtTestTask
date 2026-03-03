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

    // ── Create ──────────────────────────────────────────────────────

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

    // ── GetById ─────────────────────────────────────────────────────

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

    // ── Search: Basic ───────────────────────────────────────────────

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

    // ── Search: Case-insensitive ────────────────────────────────────

    [Fact]
    public async Task Search_CaseInsensitive_ReturnsMatches()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "UPPERCASE", Description = "", Price = 10, Category = "A" },
            new Product { Id = Guid.NewGuid(), Name = "lowercase", Description = "", Price = 20, Category = "B" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { Search = "uppercase" }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("UPPERCASE");
    }

    // ── Search: Matches description ─────────────────────────────────

    [Fact]
    public async Task Search_MatchesDescriptionField()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "Gadget", Description = "A wonderful electronic device", Price = 10, Category = "A" },
            new Product { Id = Guid.NewGuid(), Name = "Book", Description = "A paper product", Price = 20, Category = "B" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { Search = "electronic" }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Gadget");
    }

    // ── Search: Empty search string returns all ─────────────────────

    [Fact]
    public async Task Search_EmptySearchString_ReturnsAll()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "A", Description = "", Price = 10, Category = "C" },
            new Product { Id = Guid.NewGuid(), Name = "B", Description = "", Price = 20, Category = "C" },
            new Product { Id = Guid.NewGuid(), Name = "C", Description = "", Price = 30, Category = "C" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { Search = "" }, CancellationToken.None);

        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task Search_NullSearchString_ReturnsAll()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "A", Description = "", Price = 10, Category = "C" },
            new Product { Id = Guid.NewGuid(), Name = "B", Description = "", Price = 20, Category = "C" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { Search = null }, CancellationToken.None);

        result.Items.Should().HaveCount(2);
    }

    // ── Search: MinPrice only (no MaxPrice) ─────────────────────────

    [Fact]
    public async Task Search_MinPriceOnly_FiltersCorrectly()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "Cheap", Description = "", Price = 5, Category = "C" },
            new Product { Id = Guid.NewGuid(), Name = "Mid", Description = "", Price = 50, Category = "C" },
            new Product { Id = Guid.NewGuid(), Name = "Expensive", Description = "", Price = 200, Category = "C" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { MinPrice = 10 }, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(p => p.Price >= 10);
    }

    // ── Search: MaxPrice only (no MinPrice) ─────────────────────────

    [Fact]
    public async Task Search_MaxPriceOnly_FiltersCorrectly()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "Cheap", Description = "", Price = 5, Category = "C" },
            new Product { Id = Guid.NewGuid(), Name = "Mid", Description = "", Price = 50, Category = "C" },
            new Product { Id = Guid.NewGuid(), Name = "Expensive", Description = "", Price = 200, Category = "C" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { MaxPrice = 50 }, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(p => p.Price <= 50);
    }

    // ── Search: MinPrice = MaxPrice (exact price match) ─────────────

    [Fact]
    public async Task Search_MinPriceEqualsMaxPrice_ExactMatch()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "A", Description = "", Price = 10, Category = "C" },
            new Product { Id = Guid.NewGuid(), Name = "B", Description = "", Price = 50, Category = "C" },
            new Product { Id = Guid.NewGuid(), Name = "C", Description = "", Price = 100, Category = "C" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { MinPrice = 50, MaxPrice = 50 }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Price.Should().Be(50);
    }

    // ── Search: Sort by category ────────────────────────────────────

    [Fact]
    public async Task Search_SortByCategory_Works()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "Z", Description = "", Price = 10, Category = "Zebras" },
            new Product { Id = Guid.NewGuid(), Name = "A", Description = "", Price = 20, Category = "Apples" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { SortBy = "category" }, CancellationToken.None);

        result.Items[0].Category.Should().Be("Apples");
        result.Items[1].Category.Should().Be("Zebras");
    }

    // ── Search: Sort by createdAt ───────────────────────────────────

    [Fact]
    public async Task Search_SortByCreatedAt_Works()
    {
        var older = new Product
        {
            Id = Guid.NewGuid(), Name = "Old", Description = "", Price = 10, Category = "C",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        var newer = new Product
        {
            Id = Guid.NewGuid(), Name = "New", Description = "", Price = 20, Category = "C",
            CreatedAt = DateTime.UtcNow
        };
        _db.Products.AddRange(newer, older);
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { SortBy = "createdat" }, CancellationToken.None);

        result.Items[0].Name.Should().Be("Old");
        result.Items[1].Name.Should().Be("New");
    }

    // ── Search: Sort descending ─────────────────────────────────────

    [Fact]
    public async Task Search_SortDescending_Works()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "Alpha", Description = "", Price = 10, Category = "C" },
            new Product { Id = Guid.NewGuid(), Name = "Zeta", Description = "", Price = 20, Category = "C" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { SortBy = "name", SortDescending = true }, CancellationToken.None);

        result.Items[0].Name.Should().Be("Zeta");
        result.Items[1].Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task Search_SortByPriceDescending_Works()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "A", Description = "", Price = 10, Category = "C" },
            new Product { Id = Guid.NewGuid(), Name = "B", Description = "", Price = 50, Category = "C" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { SortBy = "price", SortDescending = true }, CancellationToken.None);

        result.Items[0].Price.Should().Be(50);
        result.Items[1].Price.Should().Be(10);
    }

    // ── Search: Invalid SortBy defaults to name ─────────────────────

    [Fact]
    public async Task Search_InvalidSortBy_DefaultsToName()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "Banana", Description = "", Price = 30, Category = "C" },
            new Product { Id = Guid.NewGuid(), Name = "Apple", Description = "", Price = 10, Category = "C" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { SortBy = "nonexistent_field" }, CancellationToken.None);

        result.Items[0].Name.Should().Be("Apple");
        result.Items[1].Name.Should().Be("Banana");
    }

    // ── Search: Combined filters ────────────────────────────────────

    [Fact]
    public async Task Search_CombinedFilters_Works()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "Laptop Pro", Description = "High-end laptop", Price = 1500, Category = "Electronics" },
            new Product { Id = Guid.NewGuid(), Name = "Laptop Basic", Description = "Basic laptop", Price = 500, Category = "Electronics" },
            new Product { Id = Guid.NewGuid(), Name = "Laptop Case", Description = "Case for laptop", Price = 30, Category = "Accessories" },
            new Product { Id = Guid.NewGuid(), Name = "Phone", Description = "", Price = 800, Category = "Electronics" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery
        {
            Search = "laptop",
            Category = "Electronics",
            MinPrice = 100,
            MaxPrice = 2000,
            SortBy = "price",
            SortDescending = true
        }, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items[0].Name.Should().Be("Laptop Pro");
        result.Items[1].Name.Should().Be("Laptop Basic");
    }

    // ── Search: PageSize larger than total items returns all ─────────

    [Fact]
    public async Task Search_PageSizeLargerThanTotal_ReturnsAll()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "A", Description = "", Price = 10, Category = "C" },
            new Product { Id = Guid.NewGuid(), Name = "B", Description = "", Price = 20, Category = "C" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { Page = 1, PageSize = 100 }, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.TotalPages.Should().Be(1);
    }

    // ── Search: Page beyond range returns empty ─────────────────────

    [Fact]
    public async Task Search_PageBeyondRange_ReturnsEmpty()
    {
        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "A", Description = "", Price = 10, Category = "C" },
            new Product { Id = Guid.NewGuid(), Name = "B", Description = "", Price = 20, Category = "C" });
        await _db.SaveChangesAsync();

        var handler = new SearchProductsQueryHandler(_db);
        var result = await handler.Handle(new SearchProductsQuery { Page = 99, PageSize = 10 }, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(2);
    }

    // ── Update ──────────────────────────────────────────────────────

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
    public async Task Update_UpdatedAtChanges()
    {
        var originalUpdatedAt = DateTime.UtcNow.AddDays(-1);
        var product = new Product
        {
            Id = Guid.NewGuid(), Name = "Old", Description = "Old", Price = 10, Category = "Old",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = originalUpdatedAt
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var handler = new UpdateProductCommandHandler(_db);
        var result = await handler.Handle(new UpdateProductCommand(product.Id, "New", "New", 20, "New"), CancellationToken.None);

        result.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Update_CreatedAtDoesNotChange()
    {
        var originalCreatedAt = DateTime.UtcNow.AddDays(-5);
        var product = new Product
        {
            Id = Guid.NewGuid(), Name = "Old", Description = "Old", Price = 10, Category = "Old",
            CreatedAt = originalCreatedAt,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var handler = new UpdateProductCommandHandler(_db);
        var result = await handler.Handle(new UpdateProductCommand(product.Id, "New", "New", 20, "New"), CancellationToken.None);

        result.CreatedAt.Should().BeCloseTo(originalCreatedAt, TimeSpan.FromMilliseconds(1));
    }

    // ── Delete ──────────────────────────────────────────────────────

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

    // ── Lifecycle: Create -> Update -> Delete -> GetById throws ─────

    [Fact]
    public async Task Lifecycle_CreateUpdateDeleteThenGetById_ThrowsNotFound()
    {
        // Create
        var createHandler = new CreateProductCommandHandler(_db);
        var created = await createHandler.Handle(
            new CreateProductCommand("Lifecycle", "Test product", 25m, "Testing"), CancellationToken.None);
        created.Id.Should().NotBeEmpty();

        // Update
        var updateHandler = new UpdateProductCommandHandler(_db);
        var updated = await updateHandler.Handle(
            new UpdateProductCommand(created.Id, "Updated", "Updated desc", 50m, "Updated"), CancellationToken.None);
        updated.Name.Should().Be("Updated");
        updated.Price.Should().Be(50m);

        // Delete
        var deleteHandler = new DeleteProductCommandHandler(_db);
        await deleteHandler.Handle(new DeleteProductCommand(created.Id), CancellationToken.None);

        // GetById should throw
        var getHandler = new GetProductByIdQueryHandler(_db);
        var act = () => getHandler.Handle(new GetProductByIdQuery(created.Id), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

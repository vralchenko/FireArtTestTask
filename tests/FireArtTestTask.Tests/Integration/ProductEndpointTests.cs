using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FireArtTestTask.Application.Auth.Commands;
using FireArtTestTask.Application.DTOs;
using FireArtTestTask.Application.Products.Commands;
using FireArtTestTask.Application.Products.Queries;
using FluentAssertions;

namespace FireArtTestTask.Tests.Integration;

public class ProductEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ProductEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Registers a fresh user and sets the Bearer token on _client.
    /// </summary>
    private async Task AuthenticateAsync()
    {
        var signup = new SignupCommand($"product-test-{Guid.NewGuid()}@example.com", "password123");
        var response = await _client.PostAsJsonAsync("/api/auth/signup", signup);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result!.Token);
    }

    /// <summary>
    /// Returns a NEW HttpClient (no auth headers) from the factory.
    /// </summary>
    private HttpClient CreateUnauthenticatedClient() => _factory.CreateClient();

    // ===================================================================
    // Create — happy path
    // ===================================================================

    [Fact]
    public async Task CreateProduct_Authenticated_ReturnsCreated()
    {
        await AuthenticateAsync();
        var request = new CreateProductCommand("Test Product", "A test product", 29.99m, "Electronics");

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ProductResponse>();
        result!.Name.Should().Be("Test Product");
        result.Price.Should().Be(29.99m);
    }

    [Fact]
    public async Task CreateProduct_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateUnauthenticatedClient();
        var request = new CreateProductCommand("Test", "Desc", 10m, "Cat");

        var response = await client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_InvalidData_ReturnsBadRequest()
    {
        await AuthenticateAsync();
        var request = new CreateProductCommand("", "Desc", -5m, "");

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ===================================================================
    // Create — edge cases
    // ===================================================================

    [Fact]
    public async Task CreateProduct_ResponseHasLocationHeader()
    {
        await AuthenticateAsync();
        var request = new CreateProductCommand("Location Test", "Desc", 10m, "LocCat");

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        // ASP.NET uses PascalCase controller name in the Location header ("Products" not "products")
        response.Headers.Location!.ToString().Should().ContainEquivalentOf("/api/products/");
    }

    [Fact]
    public async Task CreateProduct_ResponseIdIsValidGuid()
    {
        await AuthenticateAsync();
        var request = new CreateProductCommand("Guid Test", "Desc", 10m, "GuidCat");

        var response = await _client.PostAsJsonAsync("/api/products", request);
        var result = await response.Content.ReadFromJsonAsync<ProductResponse>();

        result!.Id.Should().NotBe(Guid.Empty);
        Guid.TryParse(result.Id.ToString(), out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateProduct_CreatedAtAndUpdatedAtAreSet()
    {
        await AuthenticateAsync();
        var before = DateTime.UtcNow.AddSeconds(-1);
        var request = new CreateProductCommand("Timestamps", "Desc", 10m, "TimeCat");

        var response = await _client.PostAsJsonAsync("/api/products", request);
        var result = await response.Content.ReadFromJsonAsync<ProductResponse>();

        result!.CreatedAt.Should().BeAfter(before);
        result.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task CreateProduct_SqlInjectionInName_StoresSafely()
    {
        await AuthenticateAsync();
        var malicious = "'; DROP TABLE Products; --";
        var request = new CreateProductCommand(malicious, "Desc", 10m, "SqlCat");

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ProductResponse>();
        result!.Name.Should().Be(malicious);
    }

    [Fact]
    public async Task CreateProduct_XssInName_StoresSafelyAsJson()
    {
        await AuthenticateAsync();
        var xss = "<script>alert('xss')</script>";
        var request = new CreateProductCommand(xss, "Desc", 10m, "XssCat");

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ProductResponse>();
        result!.Name.Should().Be(xss);
    }

    // ===================================================================
    // GetById — happy path
    // ===================================================================

    [Fact]
    public async Task GetById_ExistingProduct_ReturnsOk()
    {
        await AuthenticateAsync();
        var create = new CreateProductCommand("Get Test", "Desc", 15m, "Books");
        var createResponse = await _client.PostAsJsonAsync("/api/products", create);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var response = await _client.GetAsync($"/api/products/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProductResponse>();
        result!.Name.Should().Be("Get Test");
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ===================================================================
    // GetById — edge cases
    // ===================================================================

    [Fact]
    public async Task GetById_MalformedId_ReturnsNotFound()
    {
        await AuthenticateAsync();

        // Route constraint {id:guid} rejects "abc" — ASP.NET returns 404 (no matching route)
        var response = await _client.GetAsync("/api/products/abc");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_AllFieldsMatchCreatedProduct()
    {
        await AuthenticateAsync();
        var create = new CreateProductCommand("FieldMatch", "FieldMatchDesc", 42.42m, "FieldMatchCat");
        var createResponse = await _client.PostAsJsonAsync("/api/products", create);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var response = await _client.GetAsync($"/api/products/{created!.Id}");
        var fetched = await response.Content.ReadFromJsonAsync<ProductResponse>();

        fetched!.Id.Should().Be(created.Id);
        fetched.Name.Should().Be(created.Name);
        fetched.Description.Should().Be(created.Description);
        fetched.Price.Should().Be(created.Price);
        fetched.Category.Should().Be(created.Category);
        fetched.CreatedAt.Should().Be(created.CreatedAt);
        fetched.UpdatedAt.Should().Be(created.UpdatedAt);
    }

    // ===================================================================
    // GetById — token edge cases
    // ===================================================================

    [Fact]
    public async Task GetById_NoToken_ReturnsUnauthorized()
    {
        var client = CreateUnauthenticatedClient();

        var response = await client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_InvalidToken_ReturnsUnauthorized()
    {
        var client = CreateUnauthenticatedClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this.is.not.a.valid.jwt.token");

        var response = await client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ===================================================================
    // Search — happy path
    // ===================================================================

    [Fact]
    public async Task Search_ReturnsPagedResults()
    {
        await AuthenticateAsync();
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Search Item 1", "Desc", 10m, "Toys"));
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Search Item 2", "Desc", 20m, "Toys"));

        var response = await _client.GetAsync("/api/products?search=Search+Item&category=Toys");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        result!.Items.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    // ===================================================================
    // Search — token edge cases
    // ===================================================================

    [Fact]
    public async Task Search_NoToken_ReturnsUnauthorized()
    {
        var client = CreateUnauthenticatedClient();

        var response = await client.GetAsync("/api/products?search=anything");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ===================================================================
    // Search / Pagination — edge cases
    // ===================================================================

    [Fact]
    public async Task Search_NoQueryParams_ReturnsAllProductsWithDefaults()
    {
        await AuthenticateAsync();
        // Add a product in a unique category to verify at least one item is returned
        var uniqueCat = $"AllDef-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("AllDefault", "Desc", 10m, uniqueCat));

        var response = await _client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        result!.Items.Should().NotBeEmpty();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Search_CaseInsensitive_MatchesRegardlessOfCase()
    {
        await AuthenticateAsync();
        var uniqueCat = $"CaseTest-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("widget special", "Desc", 10m, uniqueCat));

        // Search with uppercase
        var response = await _client.GetAsync($"/api/products?search=WIDGET+SPECIAL&category={uniqueCat}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        result!.Items.Should().HaveCountGreaterThanOrEqualTo(1);
        result.Items.Should().Contain(p => p.Name == "widget special");
    }

    [Fact]
    public async Task Search_MatchesDescriptionFieldToo()
    {
        await AuthenticateAsync();
        var uniqueCat = $"DescSearch-{Guid.NewGuid():N}";
        var secretWord = $"xyzzy{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Ordinary Name", $"Description contains {secretWord}", 10m, uniqueCat));

        var response = await _client.GetAsync($"/api/products?search={secretWord}&category={uniqueCat}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        result!.Items.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Search_EmptySearchString_ReturnsAllProducts()
    {
        await AuthenticateAsync();
        var uniqueCat = $"EmptySearch-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("EmptySearchItem", "Desc", 10m, uniqueCat));

        var response = await _client.GetAsync($"/api/products?search=&category={uniqueCat}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        result!.Items.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Search_MinPriceOnly_FiltersCorrectly()
    {
        await AuthenticateAsync();
        var uniqueCat = $"MinOnly-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Cheap", "Desc", 5m, uniqueCat));
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Expensive", "Desc", 100m, uniqueCat));

        var response = await _client.GetAsync($"/api/products?minPrice=50&category={uniqueCat}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        result!.Items.Should().OnlyContain(p => p.Price >= 50m);
    }

    [Fact]
    public async Task Search_MaxPriceOnly_FiltersCorrectly()
    {
        await AuthenticateAsync();
        var uniqueCat = $"MaxOnly-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Cheap", "Desc", 5m, uniqueCat));
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Expensive", "Desc", 100m, uniqueCat));

        var response = await _client.GetAsync($"/api/products?maxPrice=50&category={uniqueCat}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        result!.Items.Should().OnlyContain(p => p.Price <= 50m);
    }

    [Fact]
    public async Task Search_MinEqualsMaxPrice_ReturnsExactPriceMatch()
    {
        await AuthenticateAsync();
        var uniqueCat = $"ExactPrice-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Exactly50", "Desc", 50m, uniqueCat));
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Not50", "Desc", 51m, uniqueCat));

        var response = await _client.GetAsync($"/api/products?minPrice=50&maxPrice=50&category={uniqueCat}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        result!.Items.Should().OnlyContain(p => p.Price == 50m);
    }

    [Fact]
    public async Task Search_SortByPriceDescending()
    {
        await AuthenticateAsync();
        var uniqueCat = $"SortPriceDesc-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Low", "Desc", 10m, uniqueCat));
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("High", "Desc", 100m, uniqueCat));
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Mid", "Desc", 50m, uniqueCat));

        var response = await _client.GetAsync($"/api/products?sortBy=price&sortDescending=true&category={uniqueCat}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        result!.Items.Should().HaveCountGreaterThanOrEqualTo(3);
        result.Items.Should().BeInDescendingOrder(p => p.Price);
    }

    [Fact]
    public async Task Search_SortByCategoryAscending()
    {
        await AuthenticateAsync();
        // Use a unique search term so we only pick up our items
        var uniquePrefix = $"SortCat{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand($"{uniquePrefix} C", "Desc", 10m, "Zebra"));
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand($"{uniquePrefix} A", "Desc", 10m, "Apple"));
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand($"{uniquePrefix} B", "Desc", 10m, "Mango"));

        var response = await _client.GetAsync($"/api/products?search={uniquePrefix}&sortBy=category&sortDescending=false");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        result!.Items.Should().HaveCountGreaterThanOrEqualTo(3);
        result.Items.Should().BeInAscendingOrder(p => p.Category);
    }

    [Fact]
    public async Task Search_InvalidSortBy_DefaultsToNameSort()
    {
        await AuthenticateAsync();
        var uniqueCat = $"InvalidSort-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Banana", "Desc", 10m, uniqueCat));
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Apple", "Desc", 10m, uniqueCat));
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Cherry", "Desc", 10m, uniqueCat));

        var response = await _client.GetAsync($"/api/products?sortBy=nonexistentField&category={uniqueCat}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        result!.Items.Should().HaveCountGreaterThanOrEqualTo(3);
        // Default sort is by name ascending
        result.Items.Should().BeInAscendingOrder(p => p.Name);
    }

    [Fact]
    public async Task Search_CombinedFilters_WorkTogether()
    {
        await AuthenticateAsync();
        var uniqueCat = $"CombinedFilter-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Alpha Widget", "Desc", 25m, uniqueCat));
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Beta Widget", "Desc", 50m, uniqueCat));
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Gamma Widget", "Desc", 75m, uniqueCat));
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Delta Gadget", "Desc", 50m, uniqueCat));

        // search=Widget + category + price range 20-60 + sort by price desc + page 1 size 2
        var response = await _client.GetAsync(
            $"/api/products?search=Widget&category={uniqueCat}&minPrice=20&maxPrice=60&sortBy=price&sortDescending=true&page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        // Alpha (25) and Beta (50) match; Gamma (75) is outside price; Delta is "Gadget" not "Widget"
        result!.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().BeInDescendingOrder(p => p.Price);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task Search_PageAndPageSizeInResponseMatchRequest()
    {
        await AuthenticateAsync();
        var uniqueCat = $"PageMatch-{Guid.NewGuid():N}";
        for (var i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/products",
                new CreateProductCommand($"PM{i}", "Desc", 10m, uniqueCat));
        }

        var response = await _client.GetAsync($"/api/products?category={uniqueCat}&page=2&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        result!.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items.Should().HaveCountLessThanOrEqualTo(2);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task Search_NonExistentPage_ReturnsEmptyItems()
    {
        await AuthenticateAsync();
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Page Test", "Desc", 10m, "Cat"));

        var response = await _client.GetAsync("/api/products?page=999&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Search_PageSizeOne_ReturnsSingleItem()
    {
        await AuthenticateAsync();
        var uniqueCat = $"PageSize1-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("PageSize1 A", "Desc", 10m, uniqueCat));
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("PageSize1 B", "Desc", 20m, uniqueCat));

        var response = await _client.GetAsync($"/api/products?category={uniqueCat}&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        result!.Items.Should().HaveCount(1);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(2);
    }

    // ===================================================================
    // Search — empty / no match
    // ===================================================================

    [Fact]
    public async Task Search_EmptyDatabase_ReturnsEmptyList()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync("/api/products?category=NonExistentCategory12345");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Search_NoMatchingPriceRange_ReturnsEmpty()
    {
        await AuthenticateAsync();
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductCommand("Cheap Item", "Desc", 5m, "PriceTest"));

        var response = await _client.GetAsync("/api/products?minPrice=1000&maxPrice=2000");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        result!.Items.Should().BeEmpty();
    }

    // ===================================================================
    // Update — happy path
    // ===================================================================

    [Fact]
    public async Task Update_ExistingProduct_ReturnsOk()
    {
        await AuthenticateAsync();
        var create = new CreateProductCommand("Old Name", "Old Desc", 10m, "Cat");
        var createResponse = await _client.PostAsJsonAsync("/api/products", create);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var update = new { Name = "New Name", Description = "New Desc", Price = 20m, Category = "NewCat" };
        var response = await _client.PutAsJsonAsync($"/api/products/{created!.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProductResponse>();
        result!.Name.Should().Be("New Name");
        result.Price.Should().Be(20m);
    }

    [Fact]
    public async Task Update_NonExistent_ReturnsNotFound()
    {
        await AuthenticateAsync();
        var update = new { Name = "Name", Description = "Desc", Price = 10m, Category = "Cat" };

        var response = await _client.PutAsJsonAsync($"/api/products/{Guid.NewGuid()}", update);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ===================================================================
    // Update — edge cases
    // ===================================================================

    [Fact]
    public async Task Update_InvalidData_ReturnsBadRequest()
    {
        await AuthenticateAsync();
        var create = new CreateProductCommand("ValidForUpdate", "Desc", 10m, "UpdCat");
        var createResponse = await _client.PostAsJsonAsync("/api/products", create);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Empty name and negative price should fail validation
        var update = new { Name = "", Description = "Desc", Price = -1m, Category = "" };
        var response = await _client.PutAsJsonAsync($"/api/products/{created!.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_UpdatedAtChangesAfterUpdate()
    {
        await AuthenticateAsync();
        var create = new CreateProductCommand("TimestampUpd", "Desc", 10m, "TimeCat");
        var createResponse = await _client.PostAsJsonAsync("/api/products", create);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Small delay to ensure UpdatedAt differs
        await Task.Delay(50);

        var update = new { Name = "TimestampUpd2", Description = "Desc2", Price = 20m, Category = "TimeCat" };
        var response = await _client.PutAsJsonAsync($"/api/products/{created!.Id}", update);
        var updated = await response.Content.ReadFromJsonAsync<ProductResponse>();

        updated!.UpdatedAt.Should().BeOnOrAfter(created.UpdatedAt);
    }

    [Fact]
    public async Task Update_CreatedAtDoesNotChangeAfterUpdate()
    {
        await AuthenticateAsync();
        var create = new CreateProductCommand("CreatedAtStable", "Desc", 10m, "StableCat");
        var createResponse = await _client.PostAsJsonAsync("/api/products", create);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var update = new { Name = "CreatedAtStable2", Description = "Desc2", Price = 20m, Category = "StableCat" };
        var response = await _client.PutAsJsonAsync($"/api/products/{created!.Id}", update);
        var updated = await response.Content.ReadFromJsonAsync<ProductResponse>();

        updated!.CreatedAt.Should().Be(created!.CreatedAt);
    }

    // ===================================================================
    // Update — token edge cases
    // ===================================================================

    [Fact]
    public async Task Update_NoToken_ReturnsUnauthorized()
    {
        var client = CreateUnauthenticatedClient();
        var update = new { Name = "X", Description = "X", Price = 10m, Category = "X" };

        var response = await client.PutAsJsonAsync($"/api/products/{Guid.NewGuid()}", update);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_InvalidToken_ReturnsUnauthorized()
    {
        var client = CreateUnauthenticatedClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "not.a.valid.jwt");
        var update = new { Name = "X", Description = "X", Price = 10m, Category = "X" };

        var response = await client.PutAsJsonAsync($"/api/products/{Guid.NewGuid()}", update);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ===================================================================
    // Delete — happy path
    // ===================================================================

    [Fact]
    public async Task Delete_ExistingProduct_ReturnsNoContent()
    {
        await AuthenticateAsync();
        var create = new CreateProductCommand("Delete Me", "Desc", 10m, "Cat");
        var createResponse = await _client.PostAsJsonAsync("/api/products", create);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var response = await _client.DeleteAsync($"/api/products/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_NonExistent_ReturnsNotFound()
    {
        await AuthenticateAsync();

        var response = await _client.DeleteAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ===================================================================
    // Delete — edge cases
    // ===================================================================

    [Fact]
    public async Task Delete_DoubleDelete_SecondReturnsNotFound()
    {
        await AuthenticateAsync();
        var create = new CreateProductCommand("DoubleDelete", "Desc", 10m, "DDCat");
        var createResponse = await _client.PostAsJsonAsync("/api/products", create);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // First delete succeeds
        var first = await _client.DeleteAsync($"/api/products/{created!.Id}");
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Second delete returns 404
        var second = await _client.DeleteAsync($"/api/products/{created.Id}");
        second.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_GetAfterDelete_ReturnsNotFound()
    {
        await AuthenticateAsync();
        var create = new CreateProductCommand("DeleteThenGet", "Desc", 10m, "DTGCat");
        var createResponse = await _client.PostAsJsonAsync("/api/products", create);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        await _client.DeleteAsync($"/api/products/{created!.Id}");

        var response = await _client.GetAsync($"/api/products/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ResponseBodyIsEmptyOn204()
    {
        await AuthenticateAsync();
        var create = new CreateProductCommand("EmptyBody", "Desc", 10m, "EBCat");
        var createResponse = await _client.PostAsJsonAsync("/api/products", create);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var response = await _client.DeleteAsync($"/api/products/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().BeNullOrEmpty();
    }

    // ===================================================================
    // Delete — token edge cases
    // ===================================================================

    [Fact]
    public async Task Delete_NoToken_ReturnsUnauthorized()
    {
        var client = CreateUnauthenticatedClient();

        var response = await client.DeleteAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_InvalidToken_ReturnsUnauthorized()
    {
        var client = CreateUnauthenticatedClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "bad.jwt.here");

        var response = await client.DeleteAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ===================================================================
    // Token edge cases for Create (kept from original)
    // ===================================================================

    [Fact]
    public async Task CreateProduct_EmptyToken_ReturnsUnauthorized()
    {
        var client = CreateUnauthenticatedClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "");

        var request = new CreateProductCommand("Test", "Desc", 10m, "Cat");
        var response = await client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_InvalidToken_ReturnsUnauthorized()
    {
        var client = CreateUnauthenticatedClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this.is.not.a.valid.jwt.token");

        var request = new CreateProductCommand("Test", "Desc", 10m, "Cat");
        var response = await client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_MalformedAuthHeader_ReturnsUnauthorized()
    {
        var client = CreateUnauthenticatedClient();
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "InvalidScheme token123");

        var request = new CreateProductCommand("Test", "Desc", 10m, "Cat");
        var response = await client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_InvalidJwt_ReturnsUnauthorized()
    {
        var client = CreateUnauthenticatedClient();
        // A structurally plausible but invalid JWT (3 base64 segments)
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.invalidsignature");

        var request = new CreateProductCommand("Test", "Desc", 10m, "Cat");
        var response = await client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ===================================================================
    // Full lifecycle test
    // ===================================================================

    [Fact]
    public async Task FullLifecycle_Create_Get_Update_Get_Delete_Get()
    {
        await AuthenticateAsync();
        var uniqueCat = $"Lifecycle-{Guid.NewGuid():N}";

        // 1. Create
        var createCmd = new CreateProductCommand("Lifecycle Item", "Original Desc", 30m, uniqueCat);
        var createResponse = await _client.PostAsJsonAsync("/api/products", createCmd);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
        created!.Name.Should().Be("Lifecycle Item");
        created.Description.Should().Be("Original Desc");
        created.Price.Should().Be(30m);
        created.Category.Should().Be(uniqueCat);

        // 2. GetById — matches created
        var getResponse1 = await _client.GetAsync($"/api/products/{created.Id}");
        getResponse1.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched1 = await getResponse1.Content.ReadFromJsonAsync<ProductResponse>();
        fetched1!.Id.Should().Be(created.Id);
        fetched1.Name.Should().Be("Lifecycle Item");

        // 3. Update
        var updatePayload = new { Name = "Updated Lifecycle", Description = "Updated Desc", Price = 55m, Category = uniqueCat };
        var updateResponse = await _client.PutAsJsonAsync($"/api/products/{created.Id}", updatePayload);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProductResponse>();
        updated!.Name.Should().Be("Updated Lifecycle");
        updated.Description.Should().Be("Updated Desc");
        updated.Price.Should().Be(55m);
        updated.CreatedAt.Should().Be(created.CreatedAt);

        // 4. GetById — reflects update
        var getResponse2 = await _client.GetAsync($"/api/products/{created.Id}");
        getResponse2.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched2 = await getResponse2.Content.ReadFromJsonAsync<ProductResponse>();
        fetched2!.Name.Should().Be("Updated Lifecycle");
        fetched2.Price.Should().Be(55m);

        // 5. Delete
        var deleteResponse = await _client.DeleteAsync($"/api/products/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 6. GetById — 404 after deletion
        var getResponse3 = await _client.GetAsync($"/api/products/{created.Id}");
        getResponse3.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

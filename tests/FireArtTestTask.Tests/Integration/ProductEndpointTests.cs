using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FireArtTestTask.Api.DTOs.Auth;
using FireArtTestTask.Api.DTOs.Products;
using FluentAssertions;

namespace FireArtTestTask.Tests.Integration;

public class ProductEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsync()
    {
        var signup = new SignupRequest($"product-test-{Guid.NewGuid()}@example.com", "password123");
        var response = await _client.PostAsJsonAsync("/api/auth/signup", signup);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result!.Token);
    }

    [Fact]
    public async Task CreateProduct_Authenticated_ReturnsCreated()
    {
        await AuthenticateAsync();
        var request = new CreateProductRequest("Test Product", "A test product", 29.99m, "Electronics");

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ProductResponse>();
        result!.Name.Should().Be("Test Product");
        result.Price.Should().Be(29.99m);
    }

    [Fact]
    public async Task CreateProduct_Unauthenticated_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var request = new CreateProductRequest("Test", "Desc", 10m, "Cat");

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_InvalidData_ReturnsBadRequest()
    {
        await AuthenticateAsync();
        var request = new CreateProductRequest("", "Desc", -5m, "");

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_ExistingProduct_ReturnsOk()
    {
        await AuthenticateAsync();
        var create = new CreateProductRequest("Get Test", "Desc", 15m, "Books");
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

    [Fact]
    public async Task Search_ReturnsPagedResults()
    {
        await AuthenticateAsync();
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("Search Item 1", "Desc", 10m, "Toys"));
        await _client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("Search Item 2", "Desc", 20m, "Toys"));

        var response = await _client.GetAsync("/api/products?search=Search+Item&category=Toys");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        result!.Items.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Update_ExistingProduct_ReturnsOk()
    {
        await AuthenticateAsync();
        var create = new CreateProductRequest("Old Name", "Old Desc", 10m, "Cat");
        var createResponse = await _client.PostAsJsonAsync("/api/products", create);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var update = new UpdateProductRequest("New Name", "New Desc", 20m, "NewCat");
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
        var update = new UpdateProductRequest("Name", "Desc", 10m, "Cat");

        var response = await _client.PutAsJsonAsync($"/api/products/{Guid.NewGuid()}", update);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ExistingProduct_ReturnsNoContent()
    {
        await AuthenticateAsync();
        var create = new CreateProductRequest("Delete Me", "Desc", 10m, "Cat");
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
}

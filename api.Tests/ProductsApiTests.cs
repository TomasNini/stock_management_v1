using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Api.Infrastructure.Data;
using Xunit;

namespace StockManagement.Api.Tests;

/// <summary>
/// HTTP tests for product CRUD. One shared ApiFactory (one in-memory DB connection);
/// the schema is wiped before every test.
/// </summary>
public class ProductsApiTests : IClassFixture<ApiFactory>, IAsyncLifetime {
    //#region Setup

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ProductsApiTests(ApiFactory factory) {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() {
        return _factory.ResetDatabase();
    }

    public Task DisposeAsync() {
        return Task.CompletedTask;
    }

    //#endregion

    //#region GET /api/products

    [Fact]
    public async Task GetProducts_WhenEmpty_ReturnsEmptyList() {
        var response = await _client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await ReadList(response);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetProducts_ReturnsAllProductsOrderedByName() {
        await CreateProduct(new ProductWriteDto("Zebra tape", "SKU-Z", null, 2, 1, 3.5m));
        await CreateProduct(new ProductWriteDto("Alpha lamp", "SKU-A", "Desk", 4, 1, 12m));

        var response = await _client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await ReadList(response);
        Assert.Equal(2, items.Count);
        Assert.Equal(["Alpha lamp", "Zebra tape"], items.Select(x => x.Name).ToArray());
    }

    //#endregion

    //#region POST /api/products

    // Create trims name / sku / description and lets SQLite assign Id.
    [Fact]
    public async Task CreateProduct_ReturnsCreatedProductAndAssignsId() {
        var response = await _client.PostAsJsonAsync("/api/products", new ProductWriteDto(
            "  Cable  ",
            "  SKU-1  ",
            "  USB-C  ",
            5,
            2,
            9.99m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await ReadOne(response);
        Assert.True(created.Id > 0);
        Assert.Equal("Cable", created.Name);
        Assert.Equal("SKU-1", created.Sku);
        Assert.Equal("USB-C", created.Description);
        Assert.Equal(5, created.Quantity);
        Assert.Equal(2, created.MinQuantity);
        Assert.Equal(9.99m, created.UnitPrice);
    }

    // Opening quantity is written as a stock movement (delta = starting qty).
    [Fact]
    public async Task CreateProduct_RecordsOpeningStockMovement() {
        var created = await CreateProduct(new ProductWriteDto("Chair", "SKU-CH", null, 8, 1, 40m));

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var movements = dbContext.StockMovements.Where(x => x.ProductId == created.Id).ToList();

        Assert.Single(movements);
        Assert.Equal(8, movements[0].Delta);
    }

    // Data annotations: empty name/sku, quantity below 1, negative min qty or price.
    [Theory]
    [InlineData("", "SKU-1", 1, 0, 1)]
    [InlineData("Name", "", 1, 0, 1)]
    [InlineData("Name", "SKU-1", 0, 0, 1)]
    [InlineData("Name", "SKU-1", 1, -1, 1)]
    [InlineData("Name", "SKU-1", 1, 0, -1)]
    public async Task CreateProduct_WhenInvalid_ReturnsBadRequest(
        string name,
        string sku,
        int quantity,
        int minQuantity,
        decimal unitPrice) {
        var response = await _client.PostAsJsonAsync(
            "/api/products",
            new ProductWriteDto(name, sku, null, quantity, minQuantity, unitPrice));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    //#endregion

    //#region GET /api/products/{id}

    [Fact]
    public async Task GetProduct_WhenExists_ReturnsProduct() {
        var created = await CreateProduct(new ProductWriteDto("Lamp", "SKU-L", "LED", 3, 1, 15m));

        var response = await _client.GetAsync($"/api/products/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = await ReadOne(response);
        Assert.Equal(created.Id, item.Id);
        Assert.Equal("Lamp", item.Name);
        Assert.Equal("SKU-L", item.Sku);
        Assert.Equal("LED", item.Description);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(1, item.MinQuantity);
        Assert.Equal(15m, item.UnitPrice);
    }

    [Fact]
    public async Task GetProduct_WhenMissing_ReturnsNotFound() {
        var response = await _client.GetAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    //#endregion

    //#region PUT /api/products/{id}

    [Fact]
    public async Task UpdateProduct_WhenExists_UpdatesFields() {
        var created = await CreateProduct(new ProductWriteDto("Old name", "SKU-OLD", "Old", 5, 1, 10m));

        var response = await _client.PutAsJsonAsync(
            $"/api/products/{created.Id}",
            new ProductWriteDto("New name", "SKU-NEW", "New", 5, 3, 12.5m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await ReadOne(await _client.GetAsync($"/api/products/{created.Id}"));
        Assert.Equal("New name", updated.Name);
        Assert.Equal("SKU-NEW", updated.Sku);
        Assert.Equal("New", updated.Description);
        Assert.Equal(5, updated.Quantity);
        Assert.Equal(3, updated.MinQuantity);
        Assert.Equal(12.5m, updated.UnitPrice);
    }

    // Quantity 10 → 7 should keep the opening +10 and add a −3 movement.
    [Fact]
    public async Task UpdateProduct_WhenQuantityChanges_RecordsMovementDelta() {
        var created = await CreateProduct(new ProductWriteDto("Bin", "SKU-B", null, 10, 1, 4m));

        var response = await _client.PutAsJsonAsync(
            $"/api/products/{created.Id}",
            new ProductWriteDto("Bin", "SKU-B", null, 7, 1, 4m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var deltas = dbContext.StockMovements
            .Where(x => x.ProductId == created.Id)
            .Select(x => x.Delta)
            .ToList();

        Assert.Contains(10, deltas);
        Assert.Contains(-3, deltas);
    }

    [Fact]
    public async Task UpdateProduct_WhenMissing_ReturnsNotFound() {
        var response = await _client.PutAsJsonAsync(
            "/api/products/999",
            new ProductWriteDto("Name", "SKU-X", null, 1, 0, 1m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_WhenInvalid_ReturnsBadRequest() {
        var created = await CreateProduct(new ProductWriteDto("Name", "SKU-V", null, 1, 0, 1m));

        var response = await _client.PutAsJsonAsync(
            $"/api/products/{created.Id}",
            new ProductWriteDto("", "SKU-V", null, 1, 0, 1m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    //#endregion

    //#region DELETE /api/products/{id}

    [Fact]
    public async Task DeleteProduct_WhenExists_RemovesProduct() {
        var created = await CreateProduct(new ProductWriteDto("Tape", "SKU-T", null, 2, 1, 1m));

        var response = await _client.DeleteAsync($"/api/products/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/products/{created.Id}")).StatusCode);
        Assert.Empty(await ReadList(await _client.GetAsync("/api/products")));
    }

    // Stock movements are cascade-deleted with the product.
    [Fact]
    public async Task DeleteProduct_WhenExists_RemovesRelatedMovements() {
        var created = await CreateProduct(new ProductWriteDto("Gloves", "SKU-G", null, 2, 1, 2m));

        await _client.DeleteAsync($"/api/products/{created.Id}");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        Assert.Empty(dbContext.StockMovements.Where(x => x.ProductId == created.Id));
    }

    [Fact]
    public async Task DeleteProduct_WhenMissing_ReturnsNotFound() {
        var response = await _client.DeleteAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    //#endregion

    //#region Helpers

    // POST then return the created product. Fails the test if create is not 200.
    private async Task<ProductResponseDto> CreateProduct(ProductWriteDto body) {
        var response = await _client.PostAsJsonAsync("/api/products", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadOne(response);
    }

    private static async Task<ProductResponseDto> ReadOne(HttpResponseMessage response) {
        var payload = await response.Content.ReadFromJsonAsync<ProductResponseDto>(JsonOptions);
        Assert.NotNull(payload);
        return payload;
    }

    private static async Task<List<ProductResponseDto>> ReadList(HttpResponseMessage response) {
        var payload = await response.Content.ReadFromJsonAsync<List<ProductResponseDto>>(JsonOptions);
        Assert.NotNull(payload);
        return payload;
    }

    //#endregion

    public record ProductWriteDto(
        string Name,
        string Sku,
        string? Description,
        int Quantity,
        int MinQuantity,
        decimal UnitPrice);

    public record ProductResponseDto(
        int Id,
        string Name,
        string Sku,
        string? Description,
        int Quantity,
        int MinQuantity,
        decimal UnitPrice,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}

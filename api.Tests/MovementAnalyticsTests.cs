using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace StockManagement.Api.Tests;

/// <summary>
/// HTTP tests for movement analytics paging. The schema is wiped before every test.
/// </summary>
public class MovementAnalyticsTests : IClassFixture<ApiFactory>, IAsyncLifetime {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public MovementAnalyticsTests(ApiFactory factory) {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() {
        return _factory.ResetDatabase();
    }

    public Task DisposeAsync() {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetMovementAnalytics_ReturnsOnlyTheRequestedPage() {
        for (var index = 1; index <= 12; index++) {
            await CreateProduct($"Product {index:D2}");
        }

        var first = await GetPage(1, 10);
        var second = await GetPage(2, 10);

        Assert.Equal(12, first.Total);
        Assert.Equal(10, first.Recent.Count);
        Assert.Equal(12, second.Total);
        Assert.Equal(2, second.Recent.Count);
        Assert.Empty(first.Recent.Select(x => x.ProductName).Intersect(second.Recent.Select(x => x.ProductName)));
    }

    [Fact]
    public async Task GetMovementAnalytics_WhenPageIsInvalid_ReturnsBadRequest() {
        var response = await _client.GetAsync("/api/analytics/get-movement-analytics?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task CreateProduct(string name) {
        var response = await _client.PostAsJsonAsync("/api/products", new {
            name,
            sku = name.Replace(" ", "-"),
            quantity = 1,
            minQuantity = 0,
            unitPrice = 1m,
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<MovementPage> GetPage(int page, int pageSize) {
        var response = await _client.GetAsync($"/api/analytics/get-movement-analytics?period=all&page={page}&pageSize={pageSize}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<MovementPage>(JsonOptions);
        Assert.NotNull(payload);
        return payload;
    }

    private sealed record MovementPage(List<MovementItem> Recent, int Total);

    private sealed record MovementItem(DateTimeOffset CreatedAt, string ProductName, int Delta);
}

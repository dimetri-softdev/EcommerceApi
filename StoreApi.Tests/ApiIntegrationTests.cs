using System.Net;
using System.Net.Http.Json;

namespace StoreApi.Tests;

public class ApiIntegrationTests(StoreApiFactory factory) : IClassFixture<StoreApiFactory>
{
    [Fact]
    public async Task GetProducts_ReturnsAnEmptyPublicCatalog()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<object>>();
        Assert.NotNull(products);
        Assert.Empty(products);
    }

    [Fact]
    public async Task CreateCategory_WithoutToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/categories",
            new { name = "Books", description = "Printed books" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreApi.Contracts;
using StoreApi.Controllers;
using StoreApi.Data;
using StoreApi.Models;

namespace StoreApi.Tests;

public class ProductsControllerTests
{
    [Fact]
    public async Task Create_WithExistingCategory_SavesProductAndReturnsCreatedResponse()
    {
        await using var dbContext = CreateDbContext();
        var category = new Category { Name = "Books", Description = "Printed books" };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();
        var controller = new ProductsController(dbContext);
        var request = new ProductRequest("API Handbook", "REST API guide", 19.99m, 12, category.Id);

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ProductResponse>(created.Value);
        Assert.Equal("API Handbook", response.Name);
        Assert.Equal("Books", response.CategoryName);
        Assert.Equal(1, await dbContext.Products.CountAsync());
    }

    [Fact]
    public async Task Create_WithUnknownCategory_ReturnsValidationError()
    {
        await using var dbContext = CreateDbContext();
        var controller = new ProductsController(dbContext);
        var request = new ProductRequest("API Handbook", "REST API guide", 19.99m, 12, 999);

        var result = await controller.Create(request, CancellationToken.None);

        var badRequest = Assert.IsType<ObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Contains(nameof(ProductRequest.CategoryId), problem.Errors.Keys);
        Assert.Empty(await dbContext.Products.ToListAsync());
    }

    private static StoreDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new StoreDbContext(options);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreApi.Contracts;
using StoreApi.Data;
using StoreApi.Models;

namespace StoreApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(StoreDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Id)
            .Select(product => new ProductResponse(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.StockQuantity,
                product.CategoryId,
                product.Category!.Name))
            .ToListAsync(cancellationToken);

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new ProductResponse(
                item.Id,
                item.Name,
                item.Description,
                item.Price,
                item.StockQuantity,
                item.CategoryId,
                item.Category!.Name))
            .SingleOrDefaultAsync(cancellationToken);

        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductResponse>> Create(
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Categories.AnyAsync(category => category.Id == request.CategoryId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.CategoryId), "The specified category does not exist.");
            return ValidationProblem(ModelState);
        }

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            CategoryId = request.CategoryId
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        var categoryName = await dbContext.Categories
            .Where(category => category.Id == product.CategoryId)
            .Select(category => category.Name)
            .SingleAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            ToResponse(product, categoryName));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id,
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FindAsync([id], cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        if (!await dbContext.Categories.AnyAsync(category => category.Id == request.CategoryId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.CategoryId), "The specified category does not exist.");
            return ValidationProblem(ModelState);
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.CategoryId = request.CategoryId;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FindAsync([id], cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        if (await dbContext.OrderItems.AnyAsync(item => item.ProductId == id, cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Product cannot be deleted",
                Detail = "The product is referenced by an existing order."
            });
        }

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static ProductResponse ToResponse(Product product, string categoryName) =>
        new(product.Id, product.Name, product.Description, product.Price,
            product.StockQuantity, product.CategoryId, categoryName);
}

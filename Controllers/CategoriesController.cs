using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreApi.Contracts;
using StoreApi.Data;
using StoreApi.Models;

namespace StoreApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(StoreDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Id)
            .Select(category => new CategoryResponse(category.Id, category.Name, category.Description))
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<CategoryResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new CategoryResponse(item.Id, item.Name, item.Description))
            .SingleOrDefaultAsync(cancellationToken);

        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryResponse>> Create(
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = new Category { Name = request.Name, Description = request.Description };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new CategoryResponse(category.Id, category.Name, category.Description);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id,
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.FindAsync([id], cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        category.Name = request.Name;
        category.Description = request.Description;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.FindAsync([id], cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        if (await dbContext.Products.AnyAsync(product => product.CategoryId == id, cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Category cannot be deleted",
                Detail = "The category still contains products."
            });
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

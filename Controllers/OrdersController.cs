using System.Security.Claims;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreApi.Contracts;
using StoreApi.Data;
using StoreApi.Models;

namespace StoreApi.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class OrdersController(StoreDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var orders = dbContext.Orders.AsNoTracking();
        if (!User.IsInRole("Admin"))
        {
            orders = orders.Where(order => order.UserId == userId);
        }

        var resultOrders = await orders
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(resultOrders.Select(ToResponse).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(item => item.Items)
            .ThenInclude(item => item.Product)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (order is null || (!User.IsInRole("Admin") && order.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier)))
        {
            return NotFound();
        }

        return Ok(ToResponse(order));
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var customerName = User.FindFirstValue(ClaimTypes.Name);
        var customerEmail = User.FindFirstValue(ClaimTypes.Email);
        if (userId is null || customerName is null || customerEmail is null)
        {
            return Unauthorized();
        }

        var requestedItems = request.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => (long)item.Quantity)
            })
            .ToList();
        if (requestedItems.Any(item => item.Quantity > int.MaxValue))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid order quantity",
                Detail = "The combined quantity for a product is too large."
            });
        }

        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var productIds = requestedItems.Select(item => item.ProductId).ToArray();
        var products = await dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        var missingProduct = requestedItems.FirstOrDefault(item => !products.ContainsKey(item.ProductId));
        if (missingProduct is not null)
        {
            ModelState.AddModelError(nameof(request.Items), $"Product {missingProduct.ProductId} does not exist.");
            return ValidationProblem(ModelState);
        }

        foreach (var item in requestedItems)
        {
            if (products[item.ProductId].StockQuantity < item.Quantity)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Insufficient stock",
                    Detail = $"Product {item.ProductId} does not have enough stock."
                });
            }
        }

        var order = new Order
        {
            UserId = userId,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            ShippingAddress = request.ShippingAddress,
            Items = requestedItems.Select(item =>
            {
                var product = products[item.ProductId];
                product.StockQuantity -= (int)item.Quantity;
                return new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = (int)item.Quantity,
                    UnitPrice = product.Price
                };
            }).ToList()
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        foreach (var item in order.Items)
        {
            item.Product = products[item.ProductId];
        }

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, ToResponse(order));
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Status))
        {
            ModelState.AddModelError(nameof(request.Status), "The specified order status is invalid.");
            return ValidationProblem(ModelState);
        }

        var order = await dbContext.Orders.FindAsync([id], cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        order.Status = request.Status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static OrderResponse ToResponse(Order order) =>
        new(
            order.Id,
            order.CustomerName,
            order.CustomerEmail,
            order.ShippingAddress,
            order.CreatedAtUtc,
            order.Status,
            order.Items.Select(item => new OrderItemResponse(
                item.ProductId,
                item.Product?.Name ?? string.Empty,
                item.Quantity,
                item.UnitPrice)).ToList());
}

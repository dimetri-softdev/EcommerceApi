using System.ComponentModel.DataAnnotations;
using StoreApi.Models;

namespace StoreApi.Contracts;

public sealed record ProductRequest(
    [property: Required, StringLength(120)] string Name,
    [property: StringLength(1000)] string Description,
    [property: Range(typeof(decimal), "0.01", "999999999.99")] decimal Price,
    [property: Range(0, int.MaxValue)] int StockQuantity,
    [property: Range(1, int.MaxValue)] int CategoryId);

public sealed record ProductResponse(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    int CategoryId,
    string CategoryName);

public sealed record CategoryRequest(
    [property: Required, StringLength(100)] string Name,
    [property: StringLength(500)] string Description);

public sealed record CategoryResponse(int Id, string Name, string Description);

public sealed record RegisterRequest(
    [property: Required, StringLength(120)] string FullName,
    [property: Required, EmailAddress, StringLength(256)] string Email,
    [property: Required, MinLength(12)] string Password);

public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public sealed record TokenResponse(string AccessToken, DateTime ExpiresAtUtc);

public sealed record CreateOrderRequest(
    [property: Required, StringLength(500)] string ShippingAddress,
    [property: Required, MinLength(1)] List<CreateOrderItemRequest> Items);

public sealed record CreateOrderItemRequest(
    [property: Range(1, int.MaxValue)] int ProductId,
    [property: Range(1, int.MaxValue)] int Quantity);

public sealed record OrderItemResponse(
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public sealed record OrderResponse(
    int Id,
    string CustomerName,
    string CustomerEmail,
    string ShippingAddress,
    DateTime CreatedAtUtc,
    OrderStatus Status,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record UpdateOrderStatusRequest(OrderStatus Status);

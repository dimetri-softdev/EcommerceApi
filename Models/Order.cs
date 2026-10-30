using System.ComponentModel.DataAnnotations;

namespace StoreApi.Models;

public class Order
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public AppUser? User { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}

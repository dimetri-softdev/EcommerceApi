using System.ComponentModel.DataAnnotations;

namespace StoreApi.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0.01", "999999999.99")]
    public decimal UnitPrice { get; set; }

    public Order? Order { get; set; }

    public Product? Product { get; set; }
}

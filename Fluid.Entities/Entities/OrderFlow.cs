using System.ComponentModel.DataAnnotations.Schema;

namespace Fluid.Entities.Entities;

public class OrderFlow
{
    public int Id { get; set; }

    [ForeignKey(nameof(Order))]
    public int OrderId { get; set; }

    [ForeignKey(nameof(OrderStatus))]
    public int OrderStatusId { get; set; }

    public int Rank { get; set; }
    public int CreatedBy { get; set; } // User ID who created this flow entry
    public int? UpdatedBy { get; set; } // User ID who last updated this flow entry
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Order Order { get; set; } = null!;
    public OrderStatus OrderStatus { get; set; } = null!;
}

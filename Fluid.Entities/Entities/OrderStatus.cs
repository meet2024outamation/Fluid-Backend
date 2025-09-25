using System.ComponentModel.DataAnnotations;

namespace Fluid.Entities.Entities;

public class OrderStatus
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    public int CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<OrderFlow> OrderFlows { get; set; } = new List<OrderFlow>();
}
using System.ComponentModel.DataAnnotations;

namespace Fluid.API.Models.OrderStatus;

public class CreateOrderStatusRequest
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;
}

public class UpdateOrderStatusRequest
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;
}

public class OrderStatusResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    
    // Statistics
    public int OrderCount { get; set; }
    public int OrderFlowCount { get; set; }
}
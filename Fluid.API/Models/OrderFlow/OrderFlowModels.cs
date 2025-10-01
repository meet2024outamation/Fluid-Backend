using System.ComponentModel.DataAnnotations;

namespace Fluid.API.Models.OrderFlow;

public class CreateOrderFlowStepRequest
{
    [Required]
    public int OrderStatusId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Rank must be greater than 0")]
    public int Rank { get; set; }

    public bool IsActive { get; set; } = true;
}

public class CreateOrderFlowRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one step is required")]
    public List<CreateOrderFlowStepRequest> Steps { get; set; } = new();
}

public class UpdateOrderFlowRequest
{
    [Required]
    public int OrderStatusId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Rank must be greater than 0")]
    public int Rank { get; set; }

    public bool IsActive { get; set; } = true;
}

public class OrderFlowResponse
{
    public int Id { get; set; }
    public int OrderStatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public bool IsActive { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    // Navigation details
    public string? OrderProjectName { get; set; }
    public string? OrderBatchName { get; set; }
}
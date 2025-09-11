using System.ComponentModel.DataAnnotations;

namespace Xtract.API.Models.Order;

public class AssignOrderRequest
{
    [Required]
    public int OrderId { get; set; }
    
    [Required]
    public int UserId { get; set; }
}

public class AssignOrderResponse
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
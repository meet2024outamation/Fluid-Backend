using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fluid.Entities.Entities;

public class Order
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Batch))]
    public int BatchId { get; set; }

    [ForeignKey(nameof(Project))]
    public int ProjectId { get; set; }

    [ForeignKey(nameof(OrderStatus))]
    [Required]
    public int OrderStatusId { get; set; } = 1; // Default to first status (Created)

    public int Priority { get; set; } = 5; // 1-10 priority scale

    public int? AssignedTo { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [Column(TypeName = "jsonb")]
    public string? ValidationErrors { get; set; } // store validation error details

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Batch Batch { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public OrderStatus OrderStatus { get; set; } = null!;
    //public User? AssignedUser { get; set; }
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<OrderData> OrderData { get; set; } = new List<OrderData>();
}
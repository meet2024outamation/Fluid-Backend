using Fluid.Entities.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fluid.Entities.Entities;

public class Batch
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Project))]
    public int ProjectId { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? FileUrl { get; set; }

    [Required]
    public BatchStatus Status { get; set; }

    public int TotalOrders { get; set; } = 0;

    public int ProcessedOrders { get; set; } = 0;

    [Column(TypeName = "text")]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CreatedByUser))]
    public int CreatedBy { get; set; }

    // Navigation properties
    public Project Client { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
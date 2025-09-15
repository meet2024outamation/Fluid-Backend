using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fluid.Entities.Entities;

public class Client
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public bool IsActive { get; set; } = true; // true = active (1), false = inactive (0)

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CreatedByUser))]
    public int CreatedBy { get; set; }

    // Navigation properties
    public User CreatedByUser { get; set; } = null!;
    public ICollection<ClientSchema> ClientSchemas { get; set; } = new List<ClientSchema>();
    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
    public ICollection<Order> WorkItems { get; set; } = new List<Order>();
    public ICollection<FieldMapping> FieldMappings { get; set; } = new List<FieldMapping>();
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Xtract.Entities.Enums;

namespace Xtract.Entities.Entities;

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
    public ClientStatus Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CreatedByUser))]
    public int CreatedBy { get; set; }

    // Navigation properties
    public User CreatedByUser { get; set; } = null!;
    public ICollection<ClientSchema> ClientSchemas { get; set; } = new List<ClientSchema>();
    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
    public ICollection<Order> WorkItems { get; set; } = new List<Order>();
    public ICollection<FieldMapping> FieldMappings { get; set; } = new List<FieldMapping>();
}
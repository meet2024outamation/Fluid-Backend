using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fluid.Entities.Entities;

public class Schema
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    //[Required]
    //[Column(TypeName = "text")]
    //public string Fields { get; set; } = string.Empty; // JSON field definitions

    public int Version { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CreatedByUser))]
    public int CreatedBy { get; set; }

    // Navigation properties
    public User CreatedByUser { get; set; } = null!;
    public ICollection<ClientSchema> ClientSchemas { get; set; } = new List<ClientSchema>();
    public ICollection<FieldMapping> FieldMappings { get; set; } = new List<FieldMapping>();
    public ICollection<SchemaField> SchemaFields { get; set; } = new List<SchemaField>();
}
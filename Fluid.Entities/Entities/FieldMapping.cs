using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fluid.Entities.Entities;

public class FieldMapping
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Project))]
    public int ProjectId { get; set; }

    [Required]
    [StringLength(255)]
    public string InputField { get; set; } = string.Empty;

    [ForeignKey(nameof(Schema))]
    public int SchemaId { get; set; }

    [ForeignKey(nameof(SchemaField))]
    [Required]
    public int SchemaFieldId { get; set; }

    [Column(TypeName = "text")]
    public string? Transformation { get; set; } // JSON transformation rules

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int CreatedBy { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
    public Schema Schema { get; set; } = null!;
    public SchemaField SchemaField { get; set; } = null!;
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fluid.Entities.Entities;

public class SchemaField
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Schema))]
    public int SchemaId { get; set; }

    [Required]
    [StringLength(100)]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string FieldLabel { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string DataType { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Format { get; set; } // mm-dd-yyyy, dd/mm/yyyy, currency, etc.

    public bool IsRequired { get; set; } = false;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Schema Schema { get; set; } = null!;
    public ICollection<OrderData> WorkItemData { get; set; } = new List<OrderData>();
    public ICollection<FieldMapping> FieldMappings { get; set; } = new List<FieldMapping>();
}
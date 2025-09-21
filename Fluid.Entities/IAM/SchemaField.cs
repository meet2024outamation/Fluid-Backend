using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fluid.Entities.IAM;

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
    public string? Format { get; set; }
    public bool IsRequired { get; set; } = false;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Schema Schema { get; set; } = null!;
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xtract.Entities.Entities;

public class FieldMapping
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Client))]
    public int ClientId { get; set; }

    [Required]
    [StringLength(255)]
    public string InputField { get; set; } = string.Empty;

    [ForeignKey(nameof(Schema))]
    public int SchemaId { get; set; }

    [Required]
    [StringLength(100)]
    public string SchemaFieldId { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? Transformation { get; set; } // JSON transformation rules

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CreatedByUser))]
    public int CreatedBy { get; set; }

    // Navigation properties
    public Client Client { get; set; } = null!;
    public Schema Schema { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
}
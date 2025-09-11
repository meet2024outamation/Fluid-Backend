using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xtract.Entities.Entities;

public class OrderData
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Order))]
    public int OrderId { get; set; }

    [ForeignKey(nameof(SchemaField))]
    public int SchemaFieldId { get; set; }

    [Column(TypeName = "text")]
    public string? ProcessedValue { get; set; } // cleaned/formatted value

    [Column(TypeName = "text")]
    public string? MetaDataValue { get; set; }
    [Column(TypeName = "decimal(5,4)")]
    public decimal? ConfidenceScore { get; set; } // AI confidence or manual certainty

    public bool IsVerified { get; set; } = false;

    [ForeignKey(nameof(VerifiedByUser))]
    public int? VerifiedBy { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public int? PageNumber { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Coordinates { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Order Order { get; set; } = null!;
    public SchemaField SchemaField { get; set; } = null!;
    public User? VerifiedByUser { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Fluid.Entities.Enums;

namespace Fluid.Entities.Entities;

public class AuditLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string TableName { get; set; } = string.Empty;

    [Required]
    public int RecordId { get; set; }

    [Required]
    public AuditAction Action { get; set; }

    [Column(TypeName = "jsonb")]
    public string? OldValues { get; set; } // JSON old values

    [Column(TypeName = "jsonb")]
    public string? NewValues { get; set; } // JSON new values

    [ForeignKey(nameof(User))]
    public int? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [Column(TypeName = "text")]
    public string? UserAgent { get; set; }

    // Navigation properties
    public User? User { get; set; }
}
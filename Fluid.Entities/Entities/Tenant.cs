using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fluid.Entities.Entities;

public class Tenant
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Identifier { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [StringLength(500)]
    public string ConnectionString { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDateTime { get; set; }

    [StringLength(100)]
    public string? DatabaseName { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Properties { get; set; } // Store additional tenant-specific configuration as JSON

    [ForeignKey(nameof(CreatedByUser))]
    public int? CreatedBy { get; set; }

    [ForeignKey(nameof(ModifiedByUser))]
    public int? ModifiedBy { get; set; }

    // Navigation properties
    public virtual User? CreatedByUser { get; set; }
    public virtual User? ModifiedByUser { get; set; }

    // Navigation property for UserRoles
    [InverseProperty("Tenant")]
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    // Unique constraint on identifier
    // This will be configured in the DbContext
}
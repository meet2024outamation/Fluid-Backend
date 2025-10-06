using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fluid.Entities.IAM;

public class RolePermission
{
    [Key]
    public int Id { get; set; }

    public int RoleId { get; set; }

    public int PermissionId { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(CreatedBy))]
    public int? CreatedById { get; set; }

    [ForeignKey(nameof(ModifiedBy))]
    public int? ModifiedById { get; set; }

    // Navigation properties
    [ForeignKey("RoleId")]
    [InverseProperty("RolePermissions")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("PermissionId")]
    [InverseProperty("RolePermissions")]
    public virtual Permission Permission { get; set; } = null!;

    [ForeignKey("CreatedById")]
    [InverseProperty("RolePermissionCreatedBies")]
    public virtual User? CreatedBy { get; set; }

    [ForeignKey("ModifiedById")]
    [InverseProperty("RolePermissionModifiedBies")]
    public virtual User? ModifiedBy { get; set; }
}
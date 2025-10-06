using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fluid.Entities.IAM;

public class Role
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }
    public bool IsForServicePrincipal { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedDateTime { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedDateTime { get; set; }

    [ForeignKey(nameof(CreatedBy))]
    public int? CreatedById { get; set; }

    [ForeignKey(nameof(ModifiedBy))]
    public int? ModifiedById { get; set; }

    // Navigation properties
    [ForeignKey("CreatedById")]
    [InverseProperty("RoleCreatedBies")]
    public virtual User? CreatedBy { get; set; }

    [ForeignKey("ModifiedById")]
    [InverseProperty("RoleModifiedBies")]
    public virtual User? ModifiedBy { get; set; }

    [InverseProperty("Role")]
    public virtual ICollection<UserRole> UserRoleUsers { get; set; } = new List<UserRole>();

    [InverseProperty("Role")]
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
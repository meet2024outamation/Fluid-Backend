using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fluid.Entities.IAM;

public class UserRole
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int RoleId { get; set; }

    [StringLength(40)]
    public string? TenantId { get; set; }

    public int? ProjectId { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? UniqueId { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; }

    public int? CreatedById { get; set; }

    public int? ModifiedById { get; set; }

    [ForeignKey("CreatedById")]
    [InverseProperty("UserRoleCreatedBies")]
    public virtual User? CreatedBy { get; set; }

    [ForeignKey("ModifiedById")]
    [InverseProperty("UserRoleModifiedBies")]
    public virtual User? ModifiedBy { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserRoleUsers")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("RoleId")]
    [InverseProperty("UserRoleUsers")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("TenantId")]
    [InverseProperty("UserRoles")]
    public virtual Tenant Tenant { get; set; } = null!;
}
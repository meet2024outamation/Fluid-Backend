using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fluid.Entities.Entities;

public class Role
{
    [Key]
    public int Id { get; set; }

    [StringLength(256)]
    [Unicode(false)]
    public string Name { get; set; } = null!;

    public bool IsEditable { get; set; }

    public bool IsForServicePrincipal { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset? CreatedDateTime { get; set; }

    public int? CreatedById { get; set; }

    public int? ModifiedById { get; set; }

    [ForeignKey("CreatedById")]
    [InverseProperty("RoleCreatedBies")]
    public virtual User? CreatedBy { get; set; }

    [ForeignKey("ModifiedById")]
    [InverseProperty("RoleModifiedBies")]
    public virtual User? ModifiedBy { get; set; }

    [InverseProperty("Role")]
    public virtual ICollection<UserRole> UserRoleUsers { get; set; } = new List<UserRole>();
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fluid.Entities.Entities;

public class Permission
{
    [Key]
    public int Id { get; set; }

    [StringLength(900)]
    public string Name { get; set; } = null!;

    [StringLength(900)]
    public string Code { get; set; } = null!;

    public bool IsForUser { get; set; }

    public bool IsForServicePrincipal { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public bool? IsForTenant { get; set; }

    public DateTimeOffset? CreatedDateTime { get; set; }

    [InverseProperty("Permission")]
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

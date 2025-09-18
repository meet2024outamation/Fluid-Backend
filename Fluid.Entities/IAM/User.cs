using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fluid.Entities.IAM;

public class User
{
    [Key]
    public int Id { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [Required]
    [StringLength(100)]
    public string AzureAdId { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(255)]
    public string LastName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLogin { get; set; }

    public bool IsActive { get; set; } = true;

    // Computed property for full name
    [NotMapped]
    public string Name => $"{FirstName} {LastName}".Trim();

    // Navigation properties for IAM context only
    [InverseProperty("User")]
    public virtual ICollection<UserRole> UserRoleUsers { get; set; } = new List<UserRole>();

    // Role-related creation/modification navigation properties
    [InverseProperty("CreatedBy")]
    public virtual ICollection<Role> RoleCreatedBies { get; set; } = new List<Role>();

    [InverseProperty("ModifiedBy")]
    public virtual ICollection<Role> RoleModifiedBies { get; set; } = new List<Role>();

    [InverseProperty("CreatedBy")]
    public virtual ICollection<UserRole> UserRoleCreatedBies { get; set; } = new List<UserRole>();

    [InverseProperty("ModifiedBy")]
    public virtual ICollection<UserRole> UserRoleModifiedBies { get; set; } = new List<UserRole>();

    // Tenant-related navigation properties
    [InverseProperty("CreatedByUser")]
    public virtual ICollection<Tenant> CreatedTenants { get; set; } = new List<Tenant>();

    [InverseProperty("ModifiedByUser")]
    public virtual ICollection<Tenant> ModifiedTenants { get; set; } = new List<Tenant>();
}
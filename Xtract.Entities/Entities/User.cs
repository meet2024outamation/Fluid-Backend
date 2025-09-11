using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xtract.Entities.Entities;

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

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLogin { get; set; }

    public bool IsActive { get; set; } = true;

    // Computed property for full name
    [NotMapped]
    public string Name => $"{FirstName} {LastName}".Trim();

    // Navigation properties for existing relationships
    public ICollection<Client> CreatedClients { get; set; } = new List<Client>();
    public ICollection<Schema> CreatedSchemas { get; set; } = new List<Schema>();
    public ICollection<Batch> CreatedBatches { get; set; } = new List<Batch>();
    public ICollection<Order> AssignedWorkItems { get; set; } = new List<Order>();
    public ICollection<FieldMapping> CreatedFieldMappings { get; set; } = new List<FieldMapping>();
    public ICollection<AuditLog> ChangedAuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<OrderData> VerifiedWorkItemData { get; set; } = new List<OrderData>();

    // Navigation properties for new role system
    [InverseProperty("User")]
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();



    // Role-related creation/modification navigation properties
    [InverseProperty("CreatedBy")]
    public virtual ICollection<Role> RoleCreatedBies { get; set; } = new List<Role>();

    [InverseProperty("ModifiedBy")]
    public virtual ICollection<Role> RoleModifiedBies { get; set; } = new List<Role>();

    [InverseProperty("CreatedBy")]
    public virtual ICollection<UserRole> UserRoleCreatedBies { get; set; } = new List<UserRole>();

    [InverseProperty("ModifiedBy")]
    public virtual ICollection<UserRole> UserRoleModifiedBies { get; set; } = new List<UserRole>();
}
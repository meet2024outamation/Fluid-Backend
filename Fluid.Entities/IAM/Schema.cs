using System.ComponentModel.DataAnnotations;

namespace Fluid.Entities.IAM;

public class Schema
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int Version { get; set; } = 1;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public int CreatedBy { get; set; }

    // Navigation properties
    public ICollection<SchemaField> SchemaFields { get; set; } = new List<SchemaField>();
}

using System.ComponentModel.DataAnnotations;

namespace Fluid.API.Models.Tenant;

public class UpdateTenantRequest
{
    public string Id { get; set; } = string.Empty;

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

    [StringLength(100)]
    public string? DatabaseName { get; set; }

    public string? Properties { get; set; }
}
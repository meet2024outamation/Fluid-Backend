using System.ComponentModel.DataAnnotations;

namespace Fluid.API.Models.IAMSchema;

public class CreateGlobalSchemaRequest
{
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public List<CreateGlobalSchemaFieldRequest> SchemaFields { get; set; } = new List<CreateGlobalSchemaFieldRequest>();
}

public class CreateGlobalSchemaFieldRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string FieldLabel { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string DataType { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Format { get; set; }

    public bool IsRequired { get; set; } = false;

    [Range(1, int.MaxValue)]
    public int DisplayOrder { get; set; } = 1;
}

/// <summary>
/// Response model for global schema
/// </summary>
public class GlobalSchemaResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public List<GlobalSchemaFieldResponse> SchemaFields { get; set; } = new List<GlobalSchemaFieldResponse>();
}

/// <summary>
/// Response model for global schema field
/// </summary>
public class GlobalSchemaFieldResponse
{
    public int Id { get; set; }
    public int SchemaId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? Format { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// List response model for global schemas
/// </summary>
public class GlobalSchemaListResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int SchemaFieldCount { get; set; }
    public int CreatedBy { get; set; }
}

/// <summary>
/// Schema copy request for tenants
/// </summary>
public class CopySchemaToTenantRequest
{
    [Required]
    public int GlobalSchemaId { get; set; }

    [Required]
    [StringLength(255)]
    public string TenantId { get; set; } = string.Empty;

    [StringLength(255)]
    public string? CustomName { get; set; }

    [StringLength(1000)]
    public string? CustomDescription { get; set; }
}

/// <summary>
/// Schema copy response
/// </summary>
public class CopySchemaToTenantResponse
{
    public int GlobalSchemaId { get; set; }
    public string GlobalSchemaName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? TenantSchemaId { get; set; }
}
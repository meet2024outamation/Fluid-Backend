using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Fluid.API.Models.Schema;

public class CreateSchemaRequest
{
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one schema field is required")]
    public List<CreateSchemaFieldRequest> SchemaFields { get; set; } = new List<CreateSchemaFieldRequest>();
}

public class CreateSchemaFieldRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string FieldLabel { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string DataType { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Format { get; set; }

    public bool IsRequired { get; set; } = false;

    [Range(1, int.MaxValue, ErrorMessage = "Display order must be greater than 0")]
    public int DisplayOrder { get; set; } = 1;
}
public class UpdateSchemaEndpointRequest
{
    [FromRoute] public int Id { get; set; }
    [FromBody] public CreateSchemaRequest UpdateRequest { get; set; } = new CreateSchemaRequest();
}
public class UpdateSchemaStatusRequest
{
    [Required]
    public bool IsActive { get; set; }
}

public class SchemaResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public List<SchemaFieldResponse> SchemaFields { get; set; } = new List<SchemaFieldResponse>();
}

public class SchemaFieldResponse
{
    public int Id { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? Format { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SchemaListResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public int SchemaFieldCount { get; set; }
}
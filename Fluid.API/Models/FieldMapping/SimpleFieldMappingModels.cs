using System.ComponentModel.DataAnnotations;

namespace Fluid.API.Models.FieldMapping;

public record CreateFieldMappingRequest(
    [Required]
    int ProjectId,
    
    [Required]
    int SchemaId,
    
    [Required]
    int SchemaFieldId,
    
    [Required]
    [StringLength(255, MinimumLength = 1)]
    string InputField,
    
    string? Transformation = null
);

public record CreateBulkFieldMappingRequest(
    [Required]
    int ProjectId,
    
    [Required]
    int SchemaId,
    
    [Required]
    [MinLength(1, ErrorMessage = "At least one field mapping is required")]
    List<FieldMappingItem> FieldMappings
);

public record FieldMappingItem(
    [Required]
    int SchemaFieldId,
    
    [Required]
    [StringLength(255, MinimumLength = 1)]
    string InputField,
    
    string? Transformation = null
);

public record FieldMappingResponse(
    int Id,
    int ProjectId,
    int SchemaId,
    int SchemaFieldId,
    string InputField,
    string? Transformation,
    DateTime CreatedAt
);

public record BulkFieldMappingResponse(
    int ProjectId,
    int SchemaId,
    int TotalMappings,
    List<FieldMappingResponse> CreatedMappings,
    List<string> Errors
);
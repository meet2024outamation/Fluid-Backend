using System.ComponentModel.DataAnnotations;

namespace Fluid.API.Models.Client;

public class AssignSchemasRequest
{
    [Required]
    public int ClientId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one schema ID is required")]
    public List<int> SchemaIds { get; set; } = new List<int>();
}

public class ClientSchemaAssignmentResponse
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int TotalAssignedSchemas { get; set; }
    public List<AssignedSchemaInfo> AssignedSchemas { get; set; } = new List<AssignedSchemaInfo>();
    public List<string> Errors { get; set; } = new List<string>();
}

public class AssignedSchemaInfo
{
    public int SchemaId { get; set; }
    public string SchemaName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime AssignedAt { get; set; }
}
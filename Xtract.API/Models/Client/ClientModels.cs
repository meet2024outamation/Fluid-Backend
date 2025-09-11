using System.ComponentModel.DataAnnotations;
using Xtract.Entities.Enums;

namespace Xtract.API.Models.Client;

public record CreateClientRequest(
    [Required]
    [StringLength(255, MinimumLength = 1)]
    string Name,
    
    [Required]
    [StringLength(50, MinimumLength = 1)]
    string Code,
    
    [Required]
    ClientStatus Status
);

public record UpdateClientRequest(
    [Required]
    [StringLength(255, MinimumLength = 1)]
    string Name,
    
    [Required]
    [StringLength(50, MinimumLength = 1)]
    string Code,
    
    [Required]
    ClientStatus Status
)
{
    public int Id { get; init; }
};

public record ClientResponse(
    int Id,
    string Name,
    string Code,
    ClientStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int CreatedBy,
    string CreatedByName
);

public record ClientListResponse(
    int Id,
    string Name,
    string Code,
    ClientStatus Status,
    DateTime CreatedAt,
    string CreatedByName
);
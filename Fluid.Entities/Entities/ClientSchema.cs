using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fluid.Entities.Entities;

public class ClientSchema
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Client))]
    public int ClientId { get; set; }

    [ForeignKey(nameof(Schema))]
    public int SchemaId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Client Client { get; set; } = null!;
    public Schema Schema { get; set; } = null!;
}
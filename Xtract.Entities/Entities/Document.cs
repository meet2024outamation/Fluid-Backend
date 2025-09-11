using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xtract.Entities.Entities;

public class Document
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(WorkItem))]
    public int WorkItemId { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Type { get; set; }

    [Required]
    [StringLength(500)]
    public string Url { get; set; } = string.Empty;

    [StringLength(500)]
    public string? BlobName { get; set; }

    public int Pages { get; set; } = 1;

    [Column(TypeName = "text")]
    public string? SearchableText { get; set; }

    public long? FileSize { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Order WorkItem { get; set; } = null!;
}
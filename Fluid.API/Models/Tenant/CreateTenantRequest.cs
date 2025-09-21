using System.ComponentModel.DataAnnotations;

namespace Fluid.API.Models.Tenant
{
    public class CreateTenantRequest
    {
        [Required]
        [StringLength(100)]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? DatabaseName { get; set; }

        public string? Properties { get; set; } = null;
    }
}

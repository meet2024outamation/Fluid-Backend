using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedKernel.Models
{
    public class AzureADConfig
    {
        public string ClientId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string Instance { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string InviteRedirectUrl { get; set; } = string.Empty;
        public string ClientAppName { get; set; } = string.Empty;
        public string MasterClientId { get; set; } = string.Empty;
        public string MasterClientSecret { get; set; } = string.Empty;
    }
}

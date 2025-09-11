using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedKernel.Models
{
    public class SmtpdetailVM
    {
        public int Id { get; set; }
        public string ServerAddress { get; set; } = null!;
        public int PortNumber { get; set; }
        public bool EnableSsl { get; set; }
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string SenderEmail { get; set; } = null!;
        public string? DisplayName { get; set; }
    }
}

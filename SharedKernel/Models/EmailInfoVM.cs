using FluentEmail.Core.Models;

namespace SharedKernel.Models
{
    public class EmailInfoVM
    {
        public List<Address> To { get; set; }
        public List<Address>? Cc { get; set; }
        public List<Address>? Bcc { get; set; }
        public bool IsBodyHtml { get; set; }
        public string Subject { get; set; } = null!;
        public string Body { get; set; } = null!;
        public EmailInfoVM()
        {
            To = new List<Address>();
            Cc = new List<Address>();
            Bcc = new List<Address>();
            Attachments = new List<Attachment>();
        }
        public bool? Priority { get; set; }
        public List<Attachment> Attachments { get; set; }
    }
}

using FluentEmail.Core.Models;
using System.Text;

namespace SharedKernel.Models
{
    public class EmailVM
    {
        /// <summary>
        /// Sender email address optional field
        /// </summary>
        public string From { get; set; } = null!;
        public List<Address> To { get; set; }
        public List<Address>? Cc { get; set; }
        public List<Address>? Bcc { get; set; }
        public bool IsBodyHtml { get; set; }
        public StringBuilder Subject { get; set; } = null!;
        public StringBuilder Body { get; set; } = null!;
        public EmailVM()
        {
            To = new List<Address>();
            Cc = new List<Address>();
            Bcc = new List<Address>();
            Attachments = new List<Attachment>();
        }
        public bool? Priority { get; set; }
        public List<Attachment> Attachments { get; set; }
        public string? TenantId { get; set; }

    }
    public class EmailTemplate
    {
        public string? From { get; set; }
        public List<Address> To { get; set; }
        public List<Address>? Cc { get; set; }
        public List<Address>? Bcc { get; set; }
        public string Subject { get; set; } = null!;
        public string Body { get; set; } = null!;
        public bool IsBodyHtml { get; set; }
        public bool? Priority { get; set; }
        public List<Attachment> Attachments { get; set; }
        public string? TenantId { get; set; }

        public EmailTemplate()
        {
            To = new List<Address>();
            Cc = new List<Address>();
            Bcc = new List<Address>();
            Attachments = new List<Attachment>();
        }

    }
    public class Attachment
    {
        public string ContentType { get; set; } = null!;
        public byte[] Data { get; set; } = null!;
        public string FileName { get; set; } = null!;
    }
}

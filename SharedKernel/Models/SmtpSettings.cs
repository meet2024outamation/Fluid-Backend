
namespace SharedKernel.Models
{
    public class SmtpSettings
    {
        public string Sender { get; set; } = String.Empty;
        public string Receiver { get; set; } = String.Empty;
        public string SmtpServer { get; set; } = String.Empty;
        public int Port { get; set; }
        public string UserName { get; set; } = String.Empty;
        public string Password { get; set; } = String.Empty;
        public bool SSL { get; set; }
        public string DeveloperEmail { get; set; } = String.Empty;
    }
}

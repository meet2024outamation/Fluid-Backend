using SharedKernel.Models;
using Microsoft.Extensions.Logging;
using Ardalis.GuardClauses;
using FluentEmail.Core;
using FluentEmail.Smtp;
using SharedKernel.Utility;
using Microsoft.Extensions.Caching.Distributed;
using FluentEmail.Core.Interfaces;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Options;
using FluentEmail.Liquid;

namespace SharedKernel.Services
{
    public class EmailServices : IEmailServices
    {
        private readonly ILogger<EmailServices> _logger;
        private readonly IDistributedCache _distributedCache;
        public EmailServices(ILogger<EmailServices> logger, IDistributedCache distributedCache)
        {
            _logger = logger;
            _distributedCache = distributedCache;
        }

        public async Task SendEmailAsync(EmailVM email)
        {
            Guard.Against.Null(email, nameof(email));

            try
            {
                var (smtpSender, fromEmail) = await GetSenderByTenant(email.TenantId);
                Email.DefaultSender = smtpSender;
                var options = Options.Create(new LiquidRendererOptions { });
                var linquidRenderer = new LiquidRenderer(options);
                Email.DefaultRenderer = linquidRenderer;

                var fluentEmail = Email
                    .From(fromEmail)
                    .To(email.To)
                    .Subject(email.Subject.ToString())
                    .Body(email.Body.ToString(), email.IsBodyHtml);

                if (email.Cc != null && email.Cc.Any())
                    fluentEmail.CC(email.Cc);

                if (email.Bcc != null && email.Bcc.Any())
                    fluentEmail.BCC(email.Bcc);

                // Attachments
                if (email.Attachments != null && email.Attachments.Count > 0)
                {
                    foreach (var attachment in email.Attachments)
                    {
                        fluentEmail.Attach(new FluentEmail.Core.Models.Attachment
                        {
                            Data = new MemoryStream(attachment.Data),
                            Filename = attachment.FileName,
                            ContentType = attachment.ContentType
                        });
                    }
                }

                var response = await fluentEmail.SendAsync();

                if (!response.Successful)
                {
                    _logger.LogError(string.Join(", ", response.ErrorMessages), "Error sending email");
                    throw new Exception("Email sending failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Unexpected error while sending email.");
                throw;
            }
        }

        private async Task<(ISender, string)> GetSenderByTenant(string? tenantId)//SMTP or SendGrid 
        {
            string smtpKey = tenantId.IsNullOrEmpty() ? "smpt_default" : $"smtpdetails_{tenantId}";
            ISender sender;
            SmtpdetailVM? smtpdetailVM = await _distributedCache.GetAsync<SmtpdetailVM>(smtpKey);
            if (smtpdetailVM == null)
            {
                _logger.LogError("Unexpected error while sending email.");
                throw new Exception("SMTP details is not found.");
            }
            sender = new SmtpSender(new SmtpClient(smtpdetailVM.ServerAddress)
            {
                Port = smtpdetailVM.PortNumber,
                Credentials = new NetworkCredential(smtpdetailVM.UserName, smtpdetailVM.Password),
                EnableSsl = smtpdetailVM.EnableSsl
            });
            //if(senderType=="SendGrid")
            //sender = new SendGridSender(smtpdetailVM.ApiKey);

            return (sender, smtpdetailVM.UserName);
        }

        public async Task SendEmailWithTemplateAsync(EmailTemplate emailTemplate, object obj)
        {
            Guard.Against.Null(emailTemplate, nameof(emailTemplate));

            try
            {
                var (smtpSender, fromEmail) = await GetSenderByTenant(emailTemplate.TenantId);
                Email.DefaultSender = smtpSender;
                var options = Options.Create(new LiquidRendererOptions { });
                var linquidRenderer = new LiquidRenderer(options);
                Email.DefaultRenderer = linquidRenderer;

                string subject = await linquidRenderer.ParseAsync(emailTemplate.Subject, obj, true);
                var fluentEmail = Email
                    .From(fromEmail)
                    .To(emailTemplate.To)
                    .Subject(subject)
                    .UsingTemplate(emailTemplate.Body, obj, isHtml: emailTemplate.IsBodyHtml);

                if (emailTemplate.Cc != null && emailTemplate.Cc.Any())
                    fluentEmail.CC(emailTemplate.Cc);

                if (emailTemplate.Bcc != null && emailTemplate.Bcc.Any())
                    fluentEmail.BCC(emailTemplate.Bcc);

                if (emailTemplate.Attachments != null && emailTemplate.Attachments.Any())
                {
                    foreach (var attachment in emailTemplate.Attachments)
                    {
                        fluentEmail.Attach(new FluentEmail.Core.Models.Attachment
                        {
                            Data = new MemoryStream(attachment.Data),
                            Filename = attachment.FileName,
                            ContentType = attachment.ContentType
                        });
                    }
                }

                // Send the email
                var response = await fluentEmail.SendAsync();

                if (!response.Successful)
                {
                    _logger.LogError(string.Join(", ", response.ErrorMessages), "Error sending email");
                    throw new Exception("Email sending failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while sending email.");
                throw;
            }
        }

    }

}

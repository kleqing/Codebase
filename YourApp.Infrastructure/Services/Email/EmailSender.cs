
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using YourApp.Application.Services.Interfaces.Email;

namespace YourApp.Infrastructure.Services.Email;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public EmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        try
        {
            string fromMail = Environment.GetEnvironmentVariable("EMAIL") 
                              ?? _configuration["EmailSettings:Email"] 
                              ?? throw new Exception("Email sender not configured");

            string fromPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") 
                                  ?? _configuration["EmailSettings:Password"] 
                                  ?? throw new Exception("Email password not configured");
            
            string host = Environment.GetEnvironmentVariable("EMAIL_HOST") 
                          ?? _configuration["EmailSettings:Host"] 
                          ?? "smtp.gmail.com";
            
            string port = Environment.GetEnvironmentVariable("EMAIL_PORT") 
                          ?? _configuration["EmailSettings:Port"] 
                          ?? "587";
            
            using var smtpClient = new SmtpClient();
            smtpClient.Host = host; // SMTP server
            smtpClient.Port = int.Parse(port); // TLS port
            smtpClient.EnableSsl = true; // Enable TLS (SSL)
            smtpClient.Credentials = new System.Net.NetworkCredential(fromMail, fromPassword);

            // Setting From , To and CC
            using var mail = new MailMessage();
            mail.From = new MailAddress(fromMail);
            mail.Subject = subject;
            mail.Body = GenerateEmailTemplate(email, subject, htmlMessage);
            mail.IsBodyHtml = true;

            mail.To.Add(new MailAddress(email));

            await smtpClient.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            // TODO: inject ILogger<EmailSender> and log instead
            Console.WriteLine($"[EmailSender] Failed: {ex.Message}");
            throw;
        }
    }

    private string GenerateEmailTemplate(string email, string subject, string htmlMsg)
    {
        return "Add your email template here" +
               $"<h3>{subject}</h3>" +
               $"<p>To: {email}</p>" +
               $"<div>{htmlMsg}</div>";
    }
}

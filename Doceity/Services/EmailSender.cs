using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace Doceity.Services // Verifica che il namespace sia corretto
{
    // Classe helper per caricare le opzioni da User Secrets / appsettings
    public class AuthMessageSenderOptions
    {
        public string? SendGridKey { get; set; }
        public string? FromEmail { get; set; }
        public string? FromName { get; set; }
    }

    // Implementazione IEmailSender con SendGrid
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        public AuthMessageSenderOptions Options { get; }

        public EmailSender(IOptions<AuthMessageSenderOptions> optionsAccessor,
                           ILogger<EmailSender> logger)
        {
            Options = optionsAccessor.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            if (string.IsNullOrEmpty(Options.SendGridKey))
            {
                _logger.LogError("SendGrid API Key (SendGrid:ApiKey) non è configurata in User Secrets.");
                throw new InvalidOperationException("Impossibile inviare email: chiave API SendGrid mancante.");
            }
            if (string.IsNullOrEmpty(Options.FromEmail) || string.IsNullOrEmpty(Options.FromName))
            {
                _logger.LogError("Email o nome mittente (EmailSender:FromEmail, EmailSender:FromName) non configurati in User Secrets.");
                throw new InvalidOperationException("Impossibile inviare email: mittente non configurato.");
            }

            await Execute(Options.SendGridKey, subject, message, toEmail);
        }

        private async Task Execute(string apiKey, string subject, string message, string toEmail)
        {
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(Options.FromEmail, Options.FromName), // Usa mittente da opzioni
                Subject = subject,
                PlainTextContent = message, // Importante avere anche plain text
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(toEmail));
            msg.SetClickTracking(false, false); // Opzionale: disabilita tracking

            try
            {
                _logger.LogInformation("Invio email a {ToEmail} con oggetto '{Subject}' tramite SendGrid...", toEmail, subject);
                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email inviata con successo a {ToEmail}. Status Code: {StatusCode}", toEmail, response.StatusCode);
                }
                else
                {
                    string responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError("Invio email fallito a {ToEmail}. Status Code: {StatusCode}, Corpo Risposta: {Body}",
                                     toEmail, response.StatusCode, responseBody);
                    throw new InvalidOperationException($"Invio email tramite SendGrid fallito con Status Code {response.StatusCode}. Dettagli: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eccezione durante l'invio email via SendGrid a {ToEmail}.", toEmail);
                throw; // Rilancia l'eccezione
            }
        }
    }
}
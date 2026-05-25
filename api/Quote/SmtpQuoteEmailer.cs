using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using api.Configuration;
using Microsoft.Extensions.Logging;

namespace api.Quote;

internal sealed class SmtpQuoteEmailer(SmtpOptions smtp, QuoteOptions quote, ILogger<SmtpQuoteEmailer> logger) {
    private readonly SmtpOptions _smtp = smtp;
    private readonly QuoteOptions _quote = quote;
    private readonly ILogger<SmtpQuoteEmailer> _logger = logger;

    /// <summary>
    /// Sends the quote summary to all configured recipients. Returns <c>true</c> if at least one
    /// email was sent successfully. Returns <c>false</c> (and logs) if SMTP is not configured or
    /// if all sends fail — the caller should still render the customer-facing receipt either way
    /// so the customer never sees an error from a failed staff notification.
    /// </summary>
    public async Task<bool> SendAsync(QuoteRequest request, QuoteResult result, CancellationToken cancellationToken) {
        if (!_smtp.IsConfigured) {
            _logger.LogWarning("SMTP not configured — quote for {Email} was NOT emailed to staff. Configure Smtp section in appsettings.", request.Email);
            return false;
        }
        if (_quote.Recipients.Length == 0) {
            _logger.LogWarning("No quote recipients configured — quote for {Email} was NOT emailed. Configure Features.Quote.Recipients.", request.Email);
            return false;
        }

        var subject = QuoteEmailRenderer.RenderSubject(request);
        var body = QuoteEmailRenderer.RenderBody(request, result);

        using var client = new SmtpClient(_smtp.Host, _smtp.Port) {
            EnableSsl = _smtp.UseStartTls,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        if (!string.IsNullOrEmpty(_smtp.Username)) {
            client.Credentials = new NetworkCredential(_smtp.Username, _smtp.Password);
        }

        using var msg = new MailMessage {
            From = new MailAddress(_smtp.From, string.IsNullOrWhiteSpace(_smtp.FromName) ? _smtp.From : _smtp.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        foreach (var to in _quote.Recipients) msg.To.Add(to);
        if (!string.IsNullOrWhiteSpace(request.Email) && MailAddress.TryCreate(request.Email, out var reply)) {
            msg.ReplyToList.Add(reply);
        }

        try {
            await client.SendMailAsync(msg, cancellationToken);
            _logger.LogInformation("Quote emailed to {Count} recipient(s) for {Email}", _quote.Recipients.Length, request.Email);
            return true;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to send quote email for {Email}", request.Email);
            return false;
        }
    }
}

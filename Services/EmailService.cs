using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;

public class EmailService : IEmailService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;

    public EmailService(
        ApplicationDbContext db,
        IConfiguration config,
        IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _config = config;
        _scopeFactory = scopeFactory;
    }

    /// <summary>Sends the initial invoice email with payment link and PDF attached.</summary>
    public async Task SendInvoiceEmailAsync(int invoiceId)
    {
        var invoice = await LoadInvoiceAsync(invoiceId);
        if (invoice == null) return;

        var subject = $"Invoice {invoice.InvoiceNumber} from InvoiceFlow";
        var body = BuildInvoiceEmailBody(invoice);

        await SendEmailAsync(invoice, subject, body, EmailTypes.NewInvoice, attachPdf: true);
    }

    /// <summary>Sends a reminder email. isSecondReminder=true sends the firmer follow-up.</summary>
    public async Task SendReminderEmailAsync(int invoiceId, bool isSecondReminder = false)
    {
        var invoice = await LoadInvoiceAsync(invoiceId);
        if (invoice == null) return;

        if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.ProofSubmitted)
            return;

        var subject = isSecondReminder
            ? $"URGENT: Invoice {invoice.InvoiceNumber} is overdue"
            : $"Reminder: Invoice {invoice.InvoiceNumber} is due soon";

        var body = BuildReminderEmailBody(invoice, isSecondReminder);

        await SendEmailAsync(invoice, subject, body, EmailTypes.Reminder, attachPdf: true);
    }

    // ── Core send ─────────────────────────────────────────────────────────────

    private async Task SendEmailAsync(
        Invoice invoice,
        string subject,
        string bodyHtml,
        EmailTypes emailType,
        bool attachPdf)
    {
        var host        = _config["Smtp:Host"]        ?? "smtp.gmail.com";
        var port        = int.Parse(_config["Smtp:Port"] ?? "587");
        var senderEmail = _config["Smtp:SenderEmail"] ?? throw new InvalidOperationException("Smtp:SenderEmail is not configured.");
        var senderName  = _config["Smtp:SenderName"]  ?? "InvoiceFlow";
        var password    = _config["Smtp:Password"]    ?? throw new InvalidOperationException("Smtp:Password is not configured.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(new MailboxAddress(invoice.Client.Name, invoice.Client.Email));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = bodyHtml };

        if (attachPdf)
        {
            // Use the internal (no userId) overload — email is sent by the system, not by a user action
            var pdfBytes = await GeneratePdfInternalAsync(invoice.Id);
            builder.Attachments.Add(
                $"{invoice.InvoiceNumber}.pdf",
                pdfBytes,
                new MimeKit.ContentType("application", "pdf"));
        }

        message.Body = builder.ToMessageBody();

        try
        {
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(senderEmail, password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(quit: true);

            // Log the successful send
            _db.EmailLogs.Add(new EmailLog
            {
                InvoiceId      = invoice.Id,
                RecipientEmail = invoice.Client.Email,
                EmailType      = emailType,
                Status         = EmailStatus.Sent,
                Attempts       = 1,
                ErrorMessage   = "",
                OccurredAt     = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log failure without crashing invoice creation flow
            try
            {
                _db.EmailLogs.Add(new EmailLog
                {
                    InvoiceId      = invoice.Id,
                    RecipientEmail = invoice.Client.Email,
                    EmailType      = emailType,
                    Status         = EmailStatus.Failed,
                    Attempts       = 1,
                    ErrorMessage   = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message,
                    OccurredAt     = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
            catch
            {
                // Ignore secondary logging errors
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Invoice?> LoadInvoiceAsync(int invoiceId)
    {
        return await _db.Invoices
            .Include(i => i.Client)
            .Include(i => i.CreatedBy)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }

    private string BuildPaymentLink(Invoice invoice)
    {
        // The hash is stored; the raw token is not. We use the hash directly in the
        // link so the backend can look it up. This is safe — SHA-256 is one-way.
        var baseUrl = _config["App:BaseUrl"] ?? "https://localhost:5001";
        return $"{baseUrl}/pay/{invoice.PaymentTokenHash}";
    }

    private string BuildInvoiceEmailBody(Invoice invoice)
    {
        var link = BuildPaymentLink(invoice);
        return $@"
<html><body style='font-family:Arial,sans-serif;color:#333;'>
  <h2>Invoice {invoice.InvoiceNumber}</h2>
  <p>Dear {invoice.Client.Name},</p>
  <p>Please find your invoice attached. Here are the details:</p>
  <table style='border-collapse:collapse;width:100%;max-width:500px;'>
    <tr><td style='padding:8px;border:1px solid #ddd;'><b>Invoice #</b></td>
        <td style='padding:8px;border:1px solid #ddd;'>{invoice.InvoiceNumber}</td></tr>
    <tr><td style='padding:8px;border:1px solid #ddd;'><b>Amount</b></td>
        <td style='padding:8px;border:1px solid #ddd;'>PKR {invoice.Amount:N2}</td></tr>
    <tr><td style='padding:8px;border:1px solid #ddd;'><b>Due Date</b></td>
        <td style='padding:8px;border:1px solid #ddd;'>{invoice.DueDate:dd MMM yyyy}</td></tr>
  </table>
  <br/>
  <p>
    <a href='{link}' style='background:#0070f3;color:#fff;padding:12px 24px;
       text-decoration:none;border-radius:4px;display:inline-block;'>
      View Invoice &amp; Pay
    </a>
  </p>
  <p style='color:#888;font-size:12px;'>This link expires on {invoice.TokenExpiresAt:dd MMM yyyy}.</p>
</body></html>";
    }

    private string BuildReminderEmailBody(Invoice invoice, bool isSecondReminder)
    {
        var link = BuildPaymentLink(invoice);
        var tone = isSecondReminder
            ? "This is a final reminder. Please settle the outstanding amount immediately to avoid any disruption."
            : "This is a friendly reminder that your invoice is due soon.";

        return $@"
<html><body style='font-family:Arial,sans-serif;color:#333;'>
  <h2>{(isSecondReminder ? "⚠️ Urgent: " : "")}Payment Reminder — {invoice.InvoiceNumber}</h2>
  <p>Dear {invoice.Client.Name},</p>
  <p>{tone}</p>
  <table style='border-collapse:collapse;width:100%;max-width:500px;'>
    <tr><td style='padding:8px;border:1px solid #ddd;'><b>Invoice #</b></td>
        <td style='padding:8px;border:1px solid #ddd;'>{invoice.InvoiceNumber}</td></tr>
    <tr><td style='padding:8px;border:1px solid #ddd;'><b>Amount Due</b></td>
        <td style='padding:8px;border:1px solid #ddd;'>PKR {invoice.Amount:N2}</td></tr>
    <tr><td style='padding:8px;border:1px solid #ddd;'><b>Due Date</b></td>
        <td style='padding:8px;border:1px solid #ddd;'>{invoice.DueDate:dd MMM yyyy}</td></tr>
  </table>
  <br/>
  <p>
    <a href='{link}' style='background:#e53e3e;color:#fff;padding:12px 24px;
       text-decoration:none;border-radius:4px;display:inline-block;'>
      Pay Now
    </a>
  </p>
</body></html>";
    }

    /// <summary>
    /// Generates PDF bytes without a userId check — for internal system use only
    /// (email sending, Hangfire reminders). Resolves a fresh InvoiceService scope
    /// to avoid circular DI.
    /// </summary>
    private async Task<byte[]> GeneratePdfInternalAsync(int invoiceId)
    {
        using var scope = _scopeFactory.CreateScope();
        var invoiceService = scope.ServiceProvider.GetRequiredService<InvoiceService>();
        return await invoiceService.GeneratePdfInternalAsync(invoiceId);
    }
}

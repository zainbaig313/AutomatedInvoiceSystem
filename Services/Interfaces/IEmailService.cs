public interface IEmailService
{
    Task SendInvoiceEmailAsync(int invoiceId);
    Task SendReminderEmailAsync(int invoiceId, bool isSecondReminder = false);
}


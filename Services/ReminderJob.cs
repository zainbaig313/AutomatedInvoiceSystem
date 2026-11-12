using Microsoft.EntityFrameworkCore;

/// <summary>
/// Scheduled daily by Hangfire. Checks invoices and sends reminder emails.
/// Rules:
///   - Due in 3 days → first reminder
///   - Overdue by 1-3 days → first reminder (if not sent yet today)
///   - Overdue by 4+ days → second (firmer) reminder
///   - On the 1st of the month → auto-create recurring invoices
/// </summary>
public class ReminderJob
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IInvoiceService _invoiceService;

    public ReminderJob(
        ApplicationDbContext db,
        IEmailService emailService,
        IInvoiceService invoiceService)
    {
        _db = db;
        _emailService = emailService;
        _invoiceService = invoiceService;
    }

    public async Task RunAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // ── 1. Send reminders ─────────────────────────────────────────────────
        var invoices = await _db.Invoices
            .Include(i => i.Client)
            .Where(i =>
                i.Status == InvoiceStatus.Pending &&
                i.Client.IsReminderEnabled)
            .ToListAsync();

        foreach (var invoice in invoices)
        {
            var daysUntilDue = invoice.DueDate.DayNumber - today.DayNumber;
            var daysOverdue = today.DayNumber - invoice.DueDate.DayNumber;

            if (daysUntilDue == 3)
            {
                // Due in 3 days — gentle reminder
                await _emailService.SendReminderEmailAsync(invoice.Id, isSecondReminder: false);
            }
            else if (daysOverdue is >= 1 and <= 3)
            {
                // 1-3 days overdue — first reminder
                await _emailService.SendReminderEmailAsync(invoice.Id, isSecondReminder: false);
            }
            else if (daysOverdue >= 4)
            {
                // 4+ days overdue — firm second reminder
                await _emailService.SendReminderEmailAsync(invoice.Id, isSecondReminder: true);
            }
        }

        // ── 2. Auto-create recurring invoices on the 1st ──────────────────────
        if (today.Day == 1)
        {
            await CreateRecurringInvoicesAsync(today);
        }
    }

    private async Task CreateRecurringInvoicesAsync(DateOnly today)
    {
        // Find paid recurring invoices from last month to use as templates
        var lastMonth = today.AddMonths(-1);

        var recurringTemplates = await _db.Invoices
            .Where(i =>
                i.IsRecurring &&
                i.IssueDate.Year == lastMonth.Year &&
                i.IssueDate.Month == lastMonth.Month)
            .ToListAsync();

        foreach (var template in recurringTemplates)
        {
            var newDto = new CreateInvoiceDto(
                template.ClientId,
                template.Description,
                template.Amount,
                today,
                today.AddDays(30),
                IsRecurring: true
            );

            await _invoiceService.CreateAsync(template.CreatedById, newDto);
        }
    }
}


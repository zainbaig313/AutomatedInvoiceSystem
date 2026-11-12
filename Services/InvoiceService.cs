using System.Security.Cryptography;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _emailService;

    public InvoiceService(ApplicationDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    public async Task<InvoiceResponseDto> CreateAsync(int userId, CreateInvoiceDto dto)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        var invoice = new Invoice
        {
            InvoiceNumber = GenerateInvoiceNumber(),
            ClientId = dto.ClientId,
            CreatedById = userId,
            Description = dto.Description,
            Amount = dto.Amount,
            IssueDate = dto.IssueDate,
            DueDate = dto.DueDate,
            Status = InvoiceStatus.Pending,
            IsRecurring = dto.IsRecurring,
            PaymentTokenHash = HashToken(rawToken),
            TokenExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        await _emailService.SendInvoiceEmailAsync(invoice.Id);

        // Load nav properties for the DTO
        await _db.Entry(invoice).Reference(i => i.Client).LoadAsync();
        return MapToDto(invoice);
    }

    // Only returns invoices created by this user
    public async Task<List<InvoiceResponseDto>> GetAllAsync(int userId)
    {
        var invoices = await _db.Invoices
            .Include(i => i.Client)
            .Where(i => i.CreatedById == userId)
            .ToListAsync();

        return invoices.Select(MapToDto).ToList();
    }

    // Ownership check included
    public async Task<InvoiceResponseDto?> GetByIdAsync(int userId, int id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Client)
            .FirstOrDefaultAsync(i => i.Id == id && i.CreatedById == userId);

        return invoice == null ? null : MapToDto(invoice);
    }

    public async Task<bool> MarkAsPaidAsync(int userId, int id)
    {
        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == id && i.CreatedById == userId);
        if (invoice == null) return false;

        invoice.Status = InvoiceStatus.Paid;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int userId, int id)
    {
        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == id && i.CreatedById == userId);
        if (invoice == null) return false;

        _db.Invoices.Remove(invoice);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Token lookup — no userId required (public endpoint, token is the proof of access).
    /// Returns only safe public fields.
    /// </summary>
    public async Task<PublicInvoiceDto?> GetByTokenAsync(string rawToken)
    {
        var hash = HashToken(rawToken);

        var invoice = await _db.Invoices
            .Include(i => i.Client)
            .Include(i => i.CreatedBy)
            .FirstOrDefaultAsync(i =>
                i.PaymentTokenHash == hash &&
                i.TokenExpiresAt > DateTime.UtcNow);

        if (invoice == null) return null;

        return new PublicInvoiceDto(
            invoice.InvoiceNumber,
            invoice.Client.Name,
            invoice.Description,
            invoice.Amount,
            invoice.DueDate,
            invoice.Status.ToString(),
            BuildPaymentInstructions(invoice.CreatedBy)
        );
    }

    // Ownership check: only the creator can download the PDF
    public async Task<byte[]> GeneratePdfAsync(int userId, int invoiceId)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Client)
            .Include(i => i.CreatedBy)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.CreatedById == userId)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        return BuildPdf(invoice);
    }

    // Internal use by EmailService (no userId scoping needed — called right after create)
    public async Task<byte[]> GeneratePdfInternalAsync(int invoiceId)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Client)
            .Include(i => i.CreatedBy)
            .FirstOrDefaultAsync(i => i.Id == invoiceId)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        return BuildPdf(invoice);
    }

    // Only reports for this user's invoices
    public async Task<byte[]> ExportMonthlyReportAsync(int userId, int year, int month)
    {
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);

        var invoices = await _db.Invoices
            .Include(i => i.Client)
            .Where(i => i.CreatedById == userId && i.IssueDate >= from && i.IssueDate <= to)
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add($"{year}-{month:D2} Report");

        var headers = new[]
        {
            "Invoice #", "Client", "Email", "Phone", "Description",
            "Amount (PKR)", "Issue Date", "Due Date", "Status"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.DarkSlateGray;
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var inv in invoices)
        {
            ws.Cell(row, 1).Value = inv.InvoiceNumber;
            ws.Cell(row, 2).Value = inv.Client.Name;
            ws.Cell(row, 3).Value = inv.Client.Email;
            ws.Cell(row, 4).Value = inv.Client.Phone;
            ws.Cell(row, 5).Value = inv.Description;
            ws.Cell(row, 6).Value = (double)inv.Amount;
            ws.Cell(row, 7).Value = inv.IssueDate.ToString("dd MMM yyyy");
            ws.Cell(row, 8).Value = inv.DueDate.ToString("dd MMM yyyy");

            var displayStatus = inv.Status == InvoiceStatus.Pending && inv.DueDate < today
                ? "Overdue" : inv.Status.ToString();
            ws.Cell(row, 9).Value = displayStatus;

            var color = displayStatus switch
            {
                "Paid"    => XLColor.LightGreen,
                "Overdue" => XLColor.LightSalmon,
                _         => XLColor.LightYellow
            };
            ws.Row(row).Style.Fill.BackgroundColor = color;
            row++;
        }

        ws.Columns().AdjustToContents();

        var paid    = invoices.Where(i => i.Status == InvoiceStatus.Paid).ToList();
        var overdue = invoices.Where(i => i.Status == InvoiceStatus.Pending && i.DueDate < today).ToList();
        var pending = invoices.Where(i => i.Status == InvoiceStatus.Pending && i.DueDate >= today).ToList();

        int s = row + 2;
        ws.Cell(s,     1).Value = "SUMMARY"; ws.Cell(s, 1).Style.Font.Bold = true;
        ws.Cell(s + 1, 1).Value = "Paid Total:";    ws.Cell(s + 1, 2).Value = (double)paid.Sum(i => i.Amount);
        ws.Cell(s + 2, 1).Value = "Pending Total:"; ws.Cell(s + 2, 2).Value = (double)pending.Sum(i => i.Amount);
        ws.Cell(s + 3, 1).Value = "Overdue Total:"; ws.Cell(s + 3, 2).Value = (double)overdue.Sum(i => i.Amount);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string HashToken(string rawToken)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawToken);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static string GenerateInvoiceNumber()
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var suffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return $"INV-{datePart}-{suffix}";
    }

    private static string BuildPaymentInstructions(User user)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(user.BankAccountNumber))
            parts.Add($"{user.Bank} Bank Account: {user.BankAccountNumber}");
        if (!string.IsNullOrWhiteSpace(user.DigitalWallet))
            parts.Add($"JazzCash/Easypaisa: {user.DigitalWallet}");
        return parts.Count > 0
            ? string.Join("  |  ", parts)
            : "Please contact us for payment details.";
    }

    private static InvoiceResponseDto MapToDto(Invoice i) =>
        new(
            i.Id,
            i.InvoiceNumber,
            i.ClientId,
            i.Client.Name,
            i.Client.Email,
            i.Client.Phone,
            i.Client.Company,
            i.Client.City,
            i.Description,
            i.Amount,
            i.IssueDate,
            i.DueDate,
            i.Status.ToString(),
            i.IsRecurring,
            i.TokenExpiresAt
        );

    private static byte[] BuildPdf(Invoice invoice)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("INVOICE").Bold().FontSize(24);
                        col.Item().Text($"# {invoice.InvoiceNumber}").FontSize(13);
                    });
                    row.ConstantItem(200).AlignRight().Column(col =>
                    {
                        col.Item().Text($"Issue Date: {invoice.IssueDate:dd MMM yyyy}");
                        col.Item().Text($"Due Date:   {invoice.DueDate:dd MMM yyyy}");
                        col.Item().Text($"Status:     {invoice.Status}");
                    });
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Item().Background("#f5f5f5").Padding(10).Column(inner =>
                    {
                        inner.Item().Text("BILL TO").Bold();
                        inner.Item().Text(invoice.Client.Name);
                        inner.Item().Text(invoice.Client.Email);
                        inner.Item().Text(invoice.Client.Phone);
                        if (!string.IsNullOrEmpty(invoice.Client.Company))
                            inner.Item().Text(invoice.Client.Company);
                        if (!string.IsNullOrEmpty(invoice.Client.City))
                            inner.Item().Text(invoice.Client.City);
                    });

                    col.Item().Height(20);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background("#333333").Padding(5)
                                .Text("Description").FontColor("#ffffff").Bold();
                            header.Cell().Background("#333333").Padding(5).AlignRight()
                                .Text("Amount (PKR)").FontColor("#ffffff").Bold();
                        });

                        table.Cell().BorderBottom(1).BorderColor("#dddddd").Padding(5)
                            .Text(invoice.Description);
                        table.Cell().BorderBottom(1).BorderColor("#dddddd").Padding(5)
                            .AlignRight().Text($"PKR {invoice.Amount:N2}");
                    });

                    col.Item().Height(10);
                    col.Item().AlignRight()
                        .Text($"Total: PKR {invoice.Amount:N2}").Bold().FontSize(14);
                    col.Item().Height(30);

                    col.Item().Background("#fffbe6").Padding(10).Column(inner =>
                    {
                        inner.Item().Text("Payment Instructions").Bold();
                        inner.Item().Text(BuildPaymentInstructions(invoice.CreatedBy));
                    });
                });

                page.Footer().AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }
}

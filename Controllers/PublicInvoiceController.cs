using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Public endpoints — no authentication required.
/// Clients reach these via the secure payment link embedded in their email.
/// </summary>
[ApiController]
[Route("api/public/invoices")]
public class PublicInvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IPaymentProofService _proofService;
    private readonly ApplicationDbContext _db;
    private readonly InvoiceService _invoiceServiceConcrete;

    public PublicInvoiceController(
        IInvoiceService invoiceService,
        IPaymentProofService proofService,
        ApplicationDbContext db,
        InvoiceService invoiceServiceConcrete)
    {
        _invoiceService = invoiceService;
        _proofService = proofService;
        _db = db;
        _invoiceServiceConcrete = invoiceServiceConcrete;
    }

    /// <summary>
    /// GET api/public/invoices/{token}
    /// Returns safe public fields only. Always returns 404 for invalid/expired tokens.
    /// </summary>
    [HttpGet("{token}")]
    public async Task<IActionResult> GetByToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return NotFound();

        var invoice = await _invoiceService.GetByTokenAsync(token);
        return invoice == null ? NotFound() : Ok(invoice);
    }

    /// <summary>
    /// GET api/public/invoices/{token}/pdf
    /// Client downloads the invoice PDF. Token acts as the authorization.
    /// </summary>
    [HttpGet("{token}/pdf")]
    public async Task<IActionResult> DownloadPdf(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return NotFound();

        var hash = HashToken(token);
        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i =>
                (i.PaymentTokenHash == hash || i.PaymentTokenHash == token) &&
                i.TokenExpiresAt > DateTime.UtcNow);

        if (invoice == null) return NotFound();

        try
        {
            // Use the internal (no userId) overload — public endpoint, token is the auth
            var pdfBytes = await _invoiceServiceConcrete.GeneratePdfInternalAsync(invoice.Id);
            return File(pdfBytes, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// POST api/public/invoices/{token}/proof
    /// Client uploads payment proof (JPG/PNG/PDF, max 5 MB).
    /// On success: invoice status → ProofSubmitted, reminders stop.
    /// </summary>
    [HttpPost("{token}/proof")]
    public async Task<IActionResult> UploadProof(string token, IFormFile file)
    {
        if (string.IsNullOrWhiteSpace(token)) return NotFound();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Please attach a file." });

        try
        {
            var proof = await _proofService.UploadAsync(token, file);
            return Ok(new
            {
                message = "Proof of payment received. The accountant will review it shortly.",
                proof
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static string HashToken(string raw)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(raw);
        return Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
    }
}

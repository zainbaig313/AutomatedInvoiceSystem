using Microsoft.EntityFrameworkCore;

public class PaymentProofService : IPaymentProofService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    private static readonly Dictionary<string, PaymentFileType> AllowedTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "image/jpeg", PaymentFileType.jpg },
            { "image/png",  PaymentFileType.png },
            { "application/pdf", PaymentFileType.pdf }
        };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public PaymentProofService(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // Called from the public (no-auth) endpoint — token is the proof of access
    public async Task<PaymentProofResponseDto> UploadAsync(string rawToken, IFormFile file)
    {
        if (!AllowedTypes.TryGetValue(file.ContentType, out var fileType))
            throw new InvalidOperationException("Only JPG, PNG, and PDF files are accepted.");

        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("File must be 5 MB or smaller.");

        var hash = HashToken(rawToken);
        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i =>
                i.PaymentTokenHash == hash &&
                i.TokenExpiresAt > DateTime.UtcNow)
            ?? throw new KeyNotFoundException("Invalid or expired payment link.");

        var uploadDir = _config["Storage:UploadPath"] ?? "uploads/proofs";
        Directory.CreateDirectory(uploadDir);

        var randomName = $"{Guid.NewGuid():N}{GetExtension(fileType)}";
        await using (var stream = File.Create(Path.Combine(uploadDir, randomName)))
            await file.CopyToAsync(stream);

        var proof = new PaymentProof
        {
            InvoiceId = invoice.Id,
            StoredFileName = randomName,
            FileType = fileType,
            FileSize = file.Length,
            UploadedAt = DateTime.UtcNow,
            ReviewStatus = ReviewStatus.Pending
        };

        _db.PaymentProofs.Add(proof);
        invoice.Status = InvoiceStatus.ProofSubmitted; // stops reminders
        await _db.SaveChangesAsync();

        return MapToDto(proof);
    }

    // Only the invoice owner can review proof
    public async Task<PaymentProofResponseDto?> ReviewAsync(int userId, int proofId, ReviewProofDto dto)
    {
        var proof = await _db.PaymentProofs
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.Id == proofId && p.Invoice.CreatedById == userId);

        if (proof == null) return null;

        proof.ReviewStatus = dto.Approved ? ReviewStatus.Approved : ReviewStatus.Rejected;
        proof.RejectReason = dto.Approved ? null : dto.RejectReason;
        proof.ReviewedAt = DateTime.UtcNow;
        proof.Invoice.Status = dto.Approved ? InvoiceStatus.Paid : InvoiceStatus.Rejected;

        await _db.SaveChangesAsync();
        return MapToDto(proof);
    }

    // Only returns proofs for invoices owned by this user
    public async Task<List<PaymentProofResponseDto>> GetByInvoiceAsync(int userId, int invoiceId)
    {
        // Verify the invoice belongs to this user
        var invoiceExists = await _db.Invoices
            .AnyAsync(i => i.Id == invoiceId && i.CreatedById == userId);

        if (!invoiceExists) return [];

        return await _db.PaymentProofs
            .Where(p => p.InvoiceId == invoiceId)
            .Select(p => new PaymentProofResponseDto(
                p.Id, p.InvoiceId, p.StoredFileName,
                p.FileType.ToString(), p.FileSize, p.UploadedAt,
                p.ReviewStatus.ToString(), p.RejectReason, p.ReviewedAt))
            .ToListAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string HashToken(string raw)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(raw);
        return Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static string GetExtension(PaymentFileType type) => type switch
    {
        PaymentFileType.jpg => ".jpg",
        PaymentFileType.png => ".png",
        PaymentFileType.pdf => ".pdf",
        _ => ""
    };

    private static PaymentProofResponseDto MapToDto(PaymentProof p) =>
        new(p.Id, p.InvoiceId, p.StoredFileName, p.FileType.ToString(),
            p.FileSize, p.UploadedAt, p.ReviewStatus.ToString(),
            p.RejectReason, p.ReviewedAt);
}

public record PaymentProofResponseDto(
    int Id,
    int InvoiceId,
    string StoredFileName,
    string FileType,
    long FileSize,
    DateTime UploadedAt,
    string ReviewStatus,
    string? RejectReason,
    DateTime? ReviewedAt
);

public record ReviewProofDto(
    bool Approved,
    string? RejectReason
);


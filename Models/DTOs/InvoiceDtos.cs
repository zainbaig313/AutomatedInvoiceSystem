public record CreateInvoiceDto(
    int ClientId,
    string Description,
    decimal Amount,
    DateOnly IssueDate,
    DateOnly DueDate,
    bool IsRecurring
);

public record UpdateInvoiceStatusDto(
    InvoiceStatus Status
);

public record InvoiceResponseDto(
    int Id,
    string InvoiceNumber,
    // Full client snapshot
    int ClientId,
    string ClientName,
    string ClientEmail,
    string ClientPhone,
    string? ClientCompany,
    string? ClientCity,
    string Description,
    decimal Amount,
    DateOnly IssueDate,
    DateOnly DueDate,
    string Status,
    bool IsRecurring,
    DateTime TokenExpiresAt
);

// Returned to anonymous clients via payment link — no internal IDs
public record PublicInvoiceDto(
    string InvoiceNumber,
    string ClientName,
    string Description,
    decimal Amount,
    DateOnly DueDate,
    string Status,
    string PaymentInstructions
);

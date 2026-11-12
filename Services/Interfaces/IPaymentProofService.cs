public interface IPaymentProofService
{
    Task<PaymentProofResponseDto> UploadAsync(string token, IFormFile file);
    Task<PaymentProofResponseDto?> ReviewAsync(int userId, int proofId, ReviewProofDto dto);
    Task<List<PaymentProofResponseDto>> GetByInvoiceAsync(int userId, int invoiceId);
}

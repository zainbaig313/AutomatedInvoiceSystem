public interface IInvoiceService
{
    Task<InvoiceResponseDto> CreateAsync(int userId, CreateInvoiceDto dto);
    Task<List<InvoiceResponseDto>> GetAllAsync(int userId);
    Task<InvoiceResponseDto?> GetByIdAsync(int userId, int id);
    Task<bool> MarkAsPaidAsync(int userId, int id);
    Task<bool> DeleteAsync(int userId, int id);
    Task<PublicInvoiceDto?> GetByTokenAsync(string token);
    Task<byte[]> GeneratePdfAsync(int userId, int invoiceId);
    Task<byte[]> ExportMonthlyReportAsync(int userId, int year, int month);
}
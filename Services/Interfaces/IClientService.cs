public interface IClientService
{
    Task<ClientResponseDto> CreateAsync(int userId, CreateClientDto dto);
    Task<List<ClientResponseDto>> GetAllAsync(int userId);
    Task<ClientResponseDto?> GetByIdAsync(int userId, int id);
    Task<ClientResponseDto?> UpdateAsync(int userId, int id, UpdateClientDto dto);
    Task<bool> DeleteAsync(int userId, int id);
    Task<List<ClientResponseDto>> BulkImportAsync(int userId, Stream fileStream, string fileName);
}

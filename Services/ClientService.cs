using System.Text;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;

public class ClientService : IClientService
{
    private readonly ApplicationDbContext _db;

    public ClientService(ApplicationDbContext db) => _db = db;

    public async Task<ClientResponseDto> CreateAsync(int userId, CreateClientDto dto)
    {
        var emailNormalized = dto.Email.Trim().ToLowerInvariant();
        var exists = await _db.Clients.AnyAsync(c => c.CreatedById == userId && c.Email.ToLower() == emailNormalized);
        if (exists)
            throw new InvalidOperationException($"You already have a client with email '{dto.Email.Trim()}'.");

        var client = new Client
        {
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone.Trim(),
            Company = dto.Company?.Trim(),
            City = dto.City?.Trim(),
            IsReminderEnabled = dto.IsReminderEnabled,
            CreatedById = userId
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return MapToDto(client);
    }

    // Only returns clients belonging to this user
    public async Task<List<ClientResponseDto>> GetAllAsync(int userId)
    {
        return await _db.Clients
            .Where(c => c.CreatedById == userId)
            .Select(c => new ClientResponseDto(
                c.Id, c.Name, c.Email, c.Phone, c.Company, c.City, c.IsReminderEnabled))
            .ToListAsync();
    }

    // Ownership check: must belong to this user
    public async Task<ClientResponseDto?> GetByIdAsync(int userId, int id)
    {
        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.CreatedById == userId);
        return client == null ? null : MapToDto(client);
    }

    public async Task<ClientResponseDto?> UpdateAsync(int userId, int id, UpdateClientDto dto)
    {
        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.CreatedById == userId);
        if (client == null) return null;

        var emailNormalized = dto.Email.Trim().ToLowerInvariant();
        var emailTaken = await _db.Clients.AnyAsync(c => c.CreatedById == userId && c.Id != id && c.Email.ToLower() == emailNormalized);
        if (emailTaken)
            throw new InvalidOperationException($"You already have another client with email '{dto.Email.Trim()}'.");

        client.Name = dto.Name.Trim();
        client.Email = dto.Email.Trim();
        client.Phone = dto.Phone.Trim();
        client.Company = dto.Company?.Trim();
        client.City = dto.City?.Trim();
        client.IsReminderEnabled = dto.IsReminderEnabled;

        await _db.SaveChangesAsync();
        return MapToDto(client);
    }

    public async Task<bool> DeleteAsync(int userId, int id)
    {
        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.CreatedById == userId);
        if (client == null) return false;

        var hasInvoices = await _db.Invoices.AnyAsync(i => i.ClientId == id);
        if (hasInvoices)
        {
            throw new InvalidOperationException("Cannot delete this client because they have existing invoices. Please delete the invoices first.");
        }

        _db.Clients.Remove(client);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Bulk import from CSV or Excel. All imported clients are owned by userId.
    /// Skips rows whose email already exists for THIS user.
    /// </summary>
    public async Task<List<ClientResponseDto>> BulkImportAsync(int userId, Stream fileStream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        List<Client> parsed = ext switch
        {
            ".csv" => ParseCsv(fileStream, userId),
            ".xlsx" or ".xls" => ParseExcel(fileStream, userId),
            _ => throw new InvalidOperationException("Only .csv, .xlsx, and .xls files are supported.")
        };

        // Skip duplicates for this user (not globally)
        var existingEmails = await _db.Clients
            .Where(c => c.CreatedById == userId)
            .Select(c => c.Email.ToLower())
            .ToHashSetAsync();

        var newClients = parsed
            .Where(c => !existingEmails.Contains(c.Email.ToLower()))
            .ToList();

        _db.Clients.AddRange(newClients);
        await _db.SaveChangesAsync();

        return newClients.Select(MapToDto).ToList();
    }

    // ── Parsers ───────────────────────────────────────────────────────────────

    private static List<Client> ParseCsv(Stream stream, int userId)
    {
        var result = new List<Client>();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        reader.ReadLine(); // skip header
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(',');
            if (cols.Length < 3) continue;

            result.Add(new Client
            {
                Name = cols[0].Trim(),
                Email = cols[1].Trim(),
                Phone = cols[2].Trim(),
                Company = cols.Length > 3 ? cols[3].Trim() : null,
                City = cols.Length > 4 ? cols[4].Trim() : null,
                IsReminderEnabled = cols.Length > 5
                    ? bool.TryParse(cols[5].Trim(), out var r) && r
                    : true,
                CreatedById = userId
            });
        }

        return result;
    }

    private static List<Client> ParseExcel(Stream stream, int userId)
    {
        System.Text.Encoding.RegisterProvider(
            System.Text.CodePagesEncodingProvider.Instance);

        var result = new List<Client>();
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
        });

        var table = dataSet.Tables[0];
        foreach (System.Data.DataRow row in table.Rows)
        {
            var name = row["Name"]?.ToString()?.Trim();
            var email = row["Email"]?.ToString()?.Trim();
            var phone = row["Phone"]?.ToString()?.Trim();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(phone))
                continue;

            var isReminder = true;
            if (table.Columns.Contains("IsReminderEnabled"))
                bool.TryParse(row["IsReminderEnabled"]?.ToString(), out isReminder);

            result.Add(new Client
            {
                Name = name,
                Email = email,
                Phone = phone,
                Company = table.Columns.Contains("Company") ? row["Company"]?.ToString()?.Trim() : null,
                City = table.Columns.Contains("City") ? row["City"]?.ToString()?.Trim() : null,
                IsReminderEnabled = isReminder,
                CreatedById = userId
            });
        }

        return result;
    }

    private static ClientResponseDto MapToDto(Client c) =>
        new(c.Id, c.Name, c.Email, c.Phone, c.Company, c.City, c.IsReminderEnabled);
}

using Microsoft.EntityFrameworkCore;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _db;
    private readonly IJwtService _jwtService;

    public UserService(ApplicationDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    // ── Auth ──────────────────────────────────────────────────────────────────

    public async Task<LoginResponse?> LoginAsync(LoginRequestDto dto)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return null;

        var token = _jwtService.GenerateToken(user);
        return new LoginResponse(user.Name, user.Email, token);
    }

    // ── User management ───────────────────────────────────────────────────────

    public async Task<UserResponseDto> CreateAsync(CreateUserDto dto)
    {
        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Bank = dto.Bank,
            BankAccountNumber = dto.BankAccountNumber,
            DigitalWallet = dto.DigitalWallet
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<UserResponseDto?> GetByIdAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserResponseDto?> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return null;

        user.Name = dto.Name;
        user.Bank = dto.Bank;
        user.BankAccountNumber = dto.BankAccountNumber;
        user.DigitalWallet = dto.DigitalWallet;

        await _db.SaveChangesAsync();
        return MapToDto(user);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static UserResponseDto MapToDto(User u) =>
        new(u.Id, u.Name, u.Email, u.Bank.ToString(), u.BankAccountNumber, u.DigitalWallet);
}

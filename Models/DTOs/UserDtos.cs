// ── Auth DTOs ─────────────────────────────────────────────────────────────────
public record LoginRequestDto(string Email, string Password);

public record LoginResponse(string Name, string Email, string Token);

// ── User DTOs ─────────────────────────────────────────────────────────────────
public record CreateUserDto(
    string Name,
    string Email,
    string Password,
    Bank Bank,
    string? BankAccountNumber,
    string? DigitalWallet
);

public record UpdateUserDto(
    string Name,
    Bank Bank,
    string? BankAccountNumber,
    string? DigitalWallet
);

public record UserResponseDto(
    int Id,
    string Name,
    string Email,
    string Bank,
    string? BankAccountNumber,
    string? DigitalWallet
);

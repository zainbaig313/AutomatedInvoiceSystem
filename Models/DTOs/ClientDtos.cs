public record CreateClientDto(
    string Name,
    string Email,
    string Phone,
    string? Company,
    string? City,
    bool IsReminderEnabled = true
);

public record UpdateClientDto(
    string Name,
    string Email,
    string Phone,
    string? Company,
    string? City,
    bool IsReminderEnabled
);

public record ClientResponseDto(
    int Id,
    string Name,
    string Email,
    string Phone,
    string? Company,
    string? City,
    bool IsReminderEnabled
);


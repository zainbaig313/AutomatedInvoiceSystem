public interface IUserService
{
    // Auth
    Task<LoginResponse?> LoginAsync(LoginRequestDto dto);

    // User management
    Task<UserResponseDto> CreateAsync(CreateUserDto dto);
    Task<UserResponseDto?> GetByIdAsync(int id);
    Task<UserResponseDto?> UpdateAsync(int id, UpdateUserDto dto);
}

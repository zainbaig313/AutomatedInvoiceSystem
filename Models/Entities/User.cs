public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;

    // Bank account details used in invoice payment instructions
    public Bank Bank { get; set; }
    public string? BankAccountNumber { get; set; }

    // JazzCash / Easypaisa number
    public string? DigitalWallet { get; set; }
}

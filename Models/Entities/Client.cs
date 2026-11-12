public class Client
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Company { get; set; }
    public string? City { get; set; }
    public bool IsReminderEnabled { get; set; }

    // The user (accountant/owner) who added this client
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
}

public class Invoice

{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = null!;


    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;


    //client fr key 
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;





    public string Description { get; set; } = null!;
    public decimal Amount { get; set; }
    
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public InvoiceStatus Status { get; set; }
    public bool IsRecurring { get; set; }
    public string PaymentTokenHash { get; set; } = null!;
    public DateTime TokenExpiresAt { get; set; }




}
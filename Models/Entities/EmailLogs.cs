public class EmailLog
{
    public int Id { get; set; }


    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;



    public string RecipientEmail { get; set; } = null!;




    public EmailTypes EmailType { get; set; }
    public EmailStatus Status { get; set; }

    public int Attempts { get; set; }

    public string? ErrorMessage { get; set; }
    public DateTime OccurredAt { get; set; }
}
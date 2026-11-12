public class PaymentProof
{
    public int Id { get; set; }


    //invoice fr key
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    
    public string StoredFileName { get; set; } = null!;
    public PaymentFileType FileType { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public ReviewStatus ReviewStatus { get; set; }
    public string? RejectReason { get; set; } 
   


    public DateTime? ReviewedAt { get; set; }

}
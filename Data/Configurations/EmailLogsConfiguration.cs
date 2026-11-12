using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class EmailLogsConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.HasKey(u=>u.Id);

        
        
        builder.HasOne(u => u.Invoice)
               .WithMany()
               .HasForeignKey(i => i.InvoiceId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RecipientEmail)
               .HasMaxLength(150)
               .IsRequired();
        
        builder.Property(u=> u.EmailType)
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasDefaultValue(EmailTypes.NewInvoice)
                .IsRequired();
        
        builder.Property(u=> u.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(EmailStatus.Pending)
                .IsRequired();


        builder.Property(u=> u.Attempts)
                .IsRequired();
        
       

        builder.Property(u => u.ErrorMessage)
                .HasMaxLength(500)
                .IsRequired();
        
        
        builder.Property(u=> u.OccurredAt)
                .HasColumnType("datetime")
                .IsRequired();
    }
}
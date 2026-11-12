using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(u=>u.Id);

        builder.Property(u=>u.InvoiceNumber)
                .HasMaxLength(30)
                .IsRequired();

        builder.HasIndex( u=>u.InvoiceNumber)
                .IsUnique();
        
        builder.HasOne(u => u.CreatedBy)
               .WithMany()
               .HasForeignKey(i => i.CreatedById)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Client)
               .WithMany()
               .HasForeignKey(i => i.ClientId)
               .OnDelete(DeleteBehavior.Restrict);


        builder.Property(u=> u.Description)
                .HasMaxLength(500)
                .IsRequired();
        
       

        builder.Property(u => u.Amount)
                .HasPrecision(18, 2)
                .IsRequired();
        
         builder.Property(u=>u.IssueDate)
                .HasColumnType("date")
                .IsRequired();

        builder.Property(u=> u.DueDate)
                .HasColumnType("date")
                .IsRequired();

        builder.Property(u=> u.Status)
                .HasMaxLength(20)
                .HasConversion<string>()
                .HasDefaultValue(InvoiceStatus.Pending)
                .IsRequired();

         builder.Property(u=>u.IsRecurring)
                .IsRequired();

        builder.Property(u=> u.PaymentTokenHash)
                .HasMaxLength(64)
                .IsRequired();

        builder.Property(u=> u.TokenExpiresAt)
                .HasColumnType("datetime")
                .IsRequired();

        
    }
}
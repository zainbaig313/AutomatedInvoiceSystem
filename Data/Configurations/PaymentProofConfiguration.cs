using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PaymentProofConfiguration : IEntityTypeConfiguration<PaymentProof>
{
    public void Configure(EntityTypeBuilder<PaymentProof> builder)
    {
        builder.HasKey(u => u.Id);

        builder.HasOne(u => u.Invoice)
                .WithMany()
                .HasForeignKey(i => i.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);


       


        builder.Property(u => u.StoredFileName)
                .HasMaxLength(200)
                .IsRequired();

        builder.Property(u => u.FileType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

        builder.Property(u => u.FileSize)
                .IsRequired();

        builder.Property(u => u.UploadedAt)
                .HasColumnType("datetime")
                .IsRequired();

        builder.Property(u => u.ReviewStatus)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

        builder.Property(u => u.RejectReason)
                .HasMaxLength(300)
                .IsRequired(false);

        

        builder.Property(u => u.ReviewedAt)
                .HasColumnType("datetime")
                .IsRequired(false);





    }
}
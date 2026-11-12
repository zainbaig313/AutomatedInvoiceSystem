using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name)
                .HasMaxLength(100)
                .IsRequired();

        builder.Property(u => u.Email)
                .HasMaxLength(150)
                .IsRequired();

        // Email is unique per user, not globally — two users could add the same client email
        // If you want global uniqueness remove this index
        builder.HasIndex(u => u.Email)
                .IsUnique();

        builder.Property(u => u.Phone)
                .HasMaxLength(20)
                .IsRequired();

        builder.Property(u => u.Company)
                .HasMaxLength(150);

        builder.Property(u => u.City)
                .HasMaxLength(50);

        builder.Property(u => u.IsReminderEnabled)
                .HasDefaultValue(true)
                .IsRequired();

        builder.HasOne(c => c.CreatedBy)
               .WithMany()
               .HasForeignKey(c => c.CreatedById)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

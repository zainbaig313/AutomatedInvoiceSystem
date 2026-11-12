using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name)
                .HasMaxLength(100)
                .IsRequired();

        builder.Property(u => u.Email)
                .HasMaxLength(150)
                .IsRequired();

        builder.HasIndex(u => u.Email)
                .IsUnique();

        builder.Property(u => u.PasswordHash)
                .IsRequired();

        builder.Property(u => u.Bank)
                .HasMaxLength(20)
                .HasConversion<string>();

        builder.Property(u => u.BankAccountNumber)
                .HasMaxLength(50);

        builder.Property(u => u.DigitalWallet)
                .HasMaxLength(20);

        builder.HasData(new User
        {
            Id = 1,
            Name = "Zain Baig",
            Email = "zain@123gmail.com",
            PasswordHash = "$2a$11$GHicG959G7.tdP8Uaaewp.tFMq1F.6zln/k.0zP3tWYnk9cdchT7i",
            Bank = Bank.HBL,
            BankAccountNumber = null,
            DigitalWallet = null
        });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcg.Payment.Infrastructure.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Domain.Payments.Payment>
{
    public void Configure(EntityTypeBuilder<Domain.Payments.Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(x => x.Id);

        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.Status).HasConversion<int>();
        builder.Property(p => p.PspReference).HasMaxLength(120);
        // Amount é calculado
        builder.Ignore(p => p.Amount);

        builder.HasMany(p => p.Items)
            .WithOne(i => i.Payment)
            .HasForeignKey(i => i.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.PspReference)
            .IsUnique(false);
    }
}
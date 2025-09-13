using Fcg.Payment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcg.Payment.Infrastructure.Configurations;

public class PaymentItemConfiguration : IEntityTypeConfiguration<PaymentItem>
{
    public void Configure(EntityTypeBuilder<PaymentItem> builder)
    {
        builder.ToTable("PaymentItems");
        builder.HasKey(p => new { p.PaymentId, p.GameId });
        builder
            .HasOne(p => p.Payment!)
            .WithMany(p => p.Items)
            .HasForeignKey(p => p.PaymentId);
    }
}
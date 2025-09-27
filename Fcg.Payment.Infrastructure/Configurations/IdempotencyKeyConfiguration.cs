using Fcg.Payment.Domain;
using Fcg.Payment.Infrastructure.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcg.Payment.Infrastructure.Configurations;

public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("Idempotency");
        builder.HasKey(i => i.Key);
    }
}
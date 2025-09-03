using Microsoft.EntityFrameworkCore;
using Payment.Api.Domain;

namespace Payment.Api.Infrastructure;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> o) : base(o) { }

    public DbSet<Domain.Payment> Payments => Set<Domain.Payment>();
    public DbSet<PaymentItem> PaymentItems => Set<PaymentItem>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<IdempotencyKey> Idempotency => Set<IdempotencyKey>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Domain.Payment>().HasKey(x => x.PaymentId);
        b.Entity<PaymentItem>().HasKey(x => new { x.PaymentId, x.GameId });
        b.Entity<PaymentItem>()
            .HasOne(x => x.Payment!)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PaymentId);

        b.Entity<Domain.Payment>().HasIndex(x => x.PspReference).IsUnique(false);
        b.Entity<IdempotencyKey>().HasKey(x => x.Key);
        b.Entity<OutboxMessage>().HasIndex(x => x.SentAt);
    }
}

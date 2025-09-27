using Fcg.Payment.Infrastructure.Messaging;
using Fcg.Payment.Infrastructure.Stores;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Payment.Infrastructure;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext() { }

    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Domain.Payments.Payment> Payments { get; set; }
    public DbSet<OutboxMessage> Outbox { get; set; }
    public DbSet<IdempotencyKey> Idempotency { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);
    }
}

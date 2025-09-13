using Fcg.Payment.Domain;
using Fcg.Payment.Domain.Common;

namespace Fcg.Payment.Infrastructure;

public class UnitOfWork(PaymentDbContext context, IPaymentRepository paymentRepository)
    : IUnitOfWork
{
    public IPaymentRepository PaymentRepository { get; } = paymentRepository;

    public async Task<int> CommitAsync(CancellationToken cancellationToken)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        context.Dispose();
    }
}
using Fcg.Payment.Domain.Common;
using Fcg.Payment.Domain.Payments;

namespace Fcg.Payment.Infrastructure;

public class UnitOfWork(PaymentDbContext context, IPaymentRepository paymentRepository, IEventModelRepository eventModelRepository)
    : IUnitOfWork
{
    public IPaymentRepository PaymentRepository { get; } = paymentRepository;
    public IEventModelRepository EventModelRepository{ get; } = eventModelRepository;

    public async Task<int> CommitAsync(CancellationToken cancellationToken)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        context.Dispose();
    }
}
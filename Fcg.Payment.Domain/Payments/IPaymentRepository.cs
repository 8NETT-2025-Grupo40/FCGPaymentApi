using Fcg.Payment.Domain.Common;

namespace Fcg.Payment.Domain.Payments
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        Task<Payment?> GetByPspReferenceAsync(string pspRef, CancellationToken cancellationToken);
    }
}
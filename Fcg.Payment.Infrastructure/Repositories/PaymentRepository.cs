using Fcg.Payment.Domain.Payments;
using Fcg.Payment.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Payment.Infrastructure.Repositories
{
    public class PaymentRepository : EFRepository<Domain.Payments.Payment>, IPaymentRepository
    {
        private readonly PaymentDbContext _paymentsDbContext;

        public PaymentRepository(PaymentDbContext paymentsDbContext) : base(paymentsDbContext)
        {
            this._paymentsDbContext = paymentsDbContext;
        }

        public Task<Domain.Payments.Payment?> GetByPspReferenceAsync(string pspRef, CancellationToken cancellationToken)
        {
            return this._paymentsDbContext.Payments
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PspReference == pspRef, cancellationToken);
        }
    }
}
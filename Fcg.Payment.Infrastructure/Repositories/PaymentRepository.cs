using Fcg.Payment.Domain;
using Fcg.Payment.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Payment.Infrastructure.Repositories
{
    public class PaymentRepository : EFRepository<Domain.Payment>, IPaymentRepository
    {
        private readonly PaymentDbContext _paymentsDbContext;

        public PaymentRepository(PaymentDbContext paymentsDbContext) : base(paymentsDbContext)
        {
            this._paymentsDbContext = paymentsDbContext;
        }

        public Task<Domain.Payment?> GetByPspReferenceAsync(string pspRef, CancellationToken cancellationToken)
        {
            return this._paymentsDbContext.Payments
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PspReference == pspRef, cancellationToken);
        }
    }
}
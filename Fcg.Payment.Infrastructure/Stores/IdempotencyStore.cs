using Fcg.Payment.Application.Ports;
using Fcg.Payment.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Payment.Infrastructure.Stores
{
    public class IdempotencyStore : IIdempotencyStore
    {
        private readonly PaymentDbContext _paymentDbContext;

        public IdempotencyStore(PaymentDbContext paymentDbContext) => this._paymentDbContext = paymentDbContext;

        public async Task<string?> GetResponseAsync(string key, CancellationToken cancellationToken)
        {
            var row = await this._paymentDbContext.Idempotency.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
            return row?.ResponseBody;
        }

        public async Task SaveResponseAsync(string key, string responseBody, CancellationToken cancellationToken)
        {
            this._paymentDbContext.Idempotency.Add(new IdempotencyKey
            {
                Key = key,
                ResponseBody = responseBody,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await this._paymentDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

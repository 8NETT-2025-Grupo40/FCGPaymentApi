using Fcg.Payment.Domain.Common.EventSourcing;
using Fcg.Payment.Domain.Payments;
using Fcg.Payment.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Fcg.Payment.Infrastructure.Repositories
{
	public class EventModelRepository : EFRepository<EventModel>, IEventModelRepository
	{
		private readonly PaymentDbContext _paymentsDbContext;

		public EventModelRepository(PaymentDbContext paymentsDbContext) : base(paymentsDbContext)
		{
			_paymentsDbContext = paymentsDbContext;
		}

		public IEnumerable<EventModel> SelectByStreamId(Guid paymentId) =>
			_paymentsDbContext.EventStore.AsNoTracking().Where(e => e.StreamId == paymentId).OrderBy(e => e.DateCreated).ToList();
	}
}
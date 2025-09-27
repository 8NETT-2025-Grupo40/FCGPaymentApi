using Fcg.Payment.Domain.Common;
using Fcg.Payment.Domain.Common.EventSourcing;

namespace Fcg.Payment.Domain.Payments;

public interface IEventModelRepository : IRepository<EventModel>
{
	IEnumerable<EventModel> SelectByStreamId(Guid streamId);
}

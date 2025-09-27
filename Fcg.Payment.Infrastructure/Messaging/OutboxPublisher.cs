using System.Text.Json;
using Fcg.Payment.Application.Ports;

namespace Fcg.Payment.Infrastructure.Messaging
{
    public class OutboxPublisher : IOutboxPublisher
    {
        private static readonly JsonSerializerOptions JsonWrite = new(JsonSerializerDefaults.Web);
        private readonly PaymentDbContext _db;

        public OutboxPublisher(PaymentDbContext db)
        {
            this._db = db;
        }

        public void Enqueue(object eventObject, string type)
        {
            var payload = JsonSerializer.Serialize(eventObject, JsonWrite);
            this._db.Outbox.Add(new OutboxMessage
            {
                Type = type,
                PayloadJson = payload
            });
        }
    }
}

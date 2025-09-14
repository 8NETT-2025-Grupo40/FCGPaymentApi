namespace Fcg.Payment.Application.Ports
{
    public interface IOutboxPublisher
    {
        void Enqueue(object eventObject, string type);
    }
}

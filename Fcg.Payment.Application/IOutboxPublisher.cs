namespace Fcg.Payment.Application
{
    public interface IOutboxPublisher
    {
        void Enqueue(object eventObject, string type);
    }
}

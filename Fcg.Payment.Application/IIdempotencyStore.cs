namespace Fcg.Payment.Application
{
    public interface IIdempotencyStore
    {
        Task<string?> GetResponseAsync(string key, CancellationToken cancellationToken);
        Task SaveResponseAsync(string key, string responseBody, CancellationToken cancellationToken);
    }

}

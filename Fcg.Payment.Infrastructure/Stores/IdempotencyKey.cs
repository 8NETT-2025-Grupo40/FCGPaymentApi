using Fcg.Payment.Domain.Common;

namespace Fcg.Payment.Infrastructure.Stores;

public class IdempotencyKey : BaseEntity
{
    public string Key { get; set; } = null!;
    public string ResponseBody { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
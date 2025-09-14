using Fcg.Payment.Domain.Common;

namespace Fcg.Payment.Infrastructure.Stores;

public class IdempotencyKey : BaseEntity
{
    public string Key { get; set; } = default!;
    public string ResponseBody { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
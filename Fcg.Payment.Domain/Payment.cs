using Fcg.Payment.Domain.Common;

namespace Fcg.Payment.Domain;

public class Payment : BaseEntity
{
    private readonly List<PaymentItem> _items = new();

    private Payment(Guid userId, string currency)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("UserId invalid");
        }

        this.Currency = (currency ?? "BRL").Trim().ToUpperInvariant();
        this.UserId = userId;
        this.Status = PaymentStatus.Pending;
    }

    public Guid UserId { get; private set; }
    public string Currency { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? PspReference { get; private set; }

    public decimal Amount => this._items.Sum(i => i.Total);

    public IReadOnlyCollection<PaymentItem> Items => this._items.AsReadOnly();

    public static Payment Create(Guid userId, string? currency = "BRL")
        => new(userId, currency ?? "BRL");

    public void AddItem(string gameId, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(gameId))
        {
            throw new DomainException("GameId is required");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("Price cannot be negative");
        }

        this._items.Add(new PaymentItem(gameId, unitPrice));
    }

    public bool MarkAsAuthorized(string pspReference)
    {
        if (this.Status == PaymentStatus.Authorized)
        {
            return false;
        }

        if (this.Status != PaymentStatus.Pending)
        {
            return false;
        }

        this.EnsurePspReference(pspReference);
        this.Status = PaymentStatus.Authorized;
        return true;
    }

    public bool MarkAsCaptured(string pspReference)
    {
        if (this.Status == PaymentStatus.Captured)
        {
            return false;
        }

        if (this.Status is PaymentStatus.Failed or PaymentStatus.Refunded)
        {
            return false;
        }

        if (this.Status is not (PaymentStatus.Pending or PaymentStatus.Authorized))
        {
            return false;
        }

        this.EnsurePspReference(pspReference);
        this.Status = PaymentStatus.Captured;
        return true;
    }

    public bool MarkAsFailed(string reason, string pspReference)
    {
        if (this.Status == PaymentStatus.Failed)
        {
            return false;
        }

        if (this.Status is PaymentStatus.Captured or PaymentStatus.Refunded)
        {
            return false;
        }

        this.EnsurePspReference(pspReference);
        this.Status = PaymentStatus.Failed;
        // opcional: armazenar reason
        return true;
    }

    public bool MarkAsRefunded(string pspReference)
    {
        if (this.Status == PaymentStatus.Refunded)
        {
            return false;
        }

        if (this.Status != PaymentStatus.Captured)
        {
            return false;
        }

        this.EnsurePspReference(pspReference);
        this.Status = PaymentStatus.Refunded;
        return true;
    }

    /// <summary>
    /// Define a referência do PSP no pagamento. Lança exceção se ausente ou conflitante.
    /// </summary>
    public void BindPspReference(string pspReference)
        => this.EnsurePspReference(pspReference);


    /// <summary>
    /// Define o PspReference apenas se ainda não estiver definido
    /// </summary>
    private void EnsurePspReference(string pspReference)
    {
        pspReference = pspReference.Trim();

        if (pspReference.Length == 0)
        {
            throw new ArgumentException("PSP reference must be provided.", nameof(pspReference));
        }

        if (string.IsNullOrWhiteSpace(this.PspReference))
        {
            this.PspReference = pspReference;
            return;
        }

        // Se já houver ref e for diferente, lança exceção pois é um conflito
        if (!string.Equals(this.PspReference, pspReference, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Conflicting PSP reference. Existing '{this.PspReference}', incoming '{pspReference}'.");
        }
    }
}
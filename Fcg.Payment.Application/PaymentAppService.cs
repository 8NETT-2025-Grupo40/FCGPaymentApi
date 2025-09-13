using System.Text.Json;
using Fcg.Payment.Domain;
using Fcg.Payment.Domain.Common;

namespace Fcg.Payment.Application;

public class PaymentAppService : IPaymentAppService
{
    private readonly IUnitOfWork _uow;
    private readonly IIdempotencyStore _idemp;

    public PaymentAppService(IUnitOfWork uow, IIdempotencyStore idemp)
    {
        this._uow = uow;
        this._idemp = idemp;
    }

    public async Task<CreatePaymentResponse> CreateAsync(
        CreatePaymentRequest req,
        string idemKey,
        IPspClient psp,
        CancellationToken cancellationToken)
    {
        // Idempotência da criação
        string? existing = await this._idemp.GetResponseAsync(idemKey, cancellationToken);
        if (existing is not null)
        {
            return JsonSerializer.Deserialize<CreatePaymentResponse>(existing)!;
        }

        // Validacões simples de entrada
        if (req is null)
        {
            throw new ArgumentNullException(nameof(req));
        }

        if (req.Items is null || !req.Items.Any())
        {
            throw new InvalidOperationException("Payment must have at least one item.");
        }

        // Cria o agregado em Pending e adiciona itens
        Domain.Payment payment = Domain.Payment.Create(req.UserId, req.Currency);

        foreach (CreatePaymentItem i in req.Items)
        {
            payment.AddItem(i.GameId, i.UnitPrice);
        }

        await this._uow.PaymentRepository.AddAsync(payment, cancellationToken);
        // Garante Id gerado/persistência antes do PSP
        await this._uow.CommitAsync(cancellationToken);

        // Cria o checkout no PSP e vincula o PSP reference no domínio
        (string checkoutUrl, string pspRef) = await psp.CreateCheckoutAsync(payment, cancellationToken);

        // Lança se vier nulo/branco ou conflitante
        payment.BindPspReference(pspRef);

        await this._uow.CommitAsync(cancellationToken);

        // Resposta + registro da idempotência
        CreatePaymentResponse resp = new(payment.Id, checkoutUrl);
        await this._idemp.SaveResponseAsync(idemKey, JsonSerializer.Serialize(resp), cancellationToken);

        return resp;
    }

    public async Task<PaymentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Domain.Payment? payment = await this._uow.PaymentRepository.GetByIdAsync(id, cancellationToken);
        return payment is null ? null : new PaymentResponse(payment);
    }
}
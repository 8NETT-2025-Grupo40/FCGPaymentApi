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
        // Idempotência de criação
        string? existing = await this._idemp.GetResponseAsync(idemKey, cancellationToken);
        if (existing is not null)
            return JsonSerializer.Deserialize<CreatePaymentResponse>(existing)!;

        // Cria o pagamento (Pending)
        Domain.Payment payment = new(
            userId: req.UserId,
            amount: req.Items.Sum(i => i.UnitPrice),
            currency: req.Currency,
            items: req.Items.Select(i => new PaymentItem
            {
                GameId = i.GameId,
                UnitPrice = i.UnitPrice
            }).ToList());

        await this._uow.PaymentRepository.AddAsync(payment, cancellationToken);
        await this._uow.CommitAsync(cancellationToken);

        // Gera um checkoutUrl "fake" via PSP adapter atual
        (string checkoutUrl, string pspRef) = await psp.CreateCheckoutAsync(payment, cancellationToken);
        payment.PspReference = pspRef;

        await this._uow.CommitAsync(cancellationToken);

        // Resposta + registro da idempotência
        CreatePaymentResponse resp = new(payment.Id, checkoutUrl);

        await this._idemp.SaveResponseAsync(idemKey, JsonSerializer.Serialize(resp), cancellationToken);

        return resp;
    }

    public async Task<PaymentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Domain.Payment? payment = await this._uow.PaymentRepository.GetByIdAsync(id, cancellationToken);

        if (payment == null)
        {
            return null;
        }


        return new PaymentResponse(payment);
    }
}

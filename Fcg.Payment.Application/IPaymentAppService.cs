namespace Fcg.Payment.Application;

public interface IPaymentAppService
{
    Task<CreatePaymentResponse> CreateAsync(
        CreatePaymentRequest req,
        string idemKey,
        IPspClient psp,
        CancellationToken cancellationToken);

    /// <summary>
    /// Método responsável por obter um pagamento pelo seu ID.
    /// </summary>
    Task<PaymentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
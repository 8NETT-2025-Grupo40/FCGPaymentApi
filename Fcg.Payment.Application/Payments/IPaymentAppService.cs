using Fcg.Payment.Application.Events;
using Fcg.Payment.Application.Payments.Dtos;
using Fcg.Payment.Application.Ports;

namespace Fcg.Payment.Application.Payments;

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
	EventsResponse? GetEventsByPaymentId(Guid id);
}
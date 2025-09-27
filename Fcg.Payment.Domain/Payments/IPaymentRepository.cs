using Fcg.Payment.Domain.Common;

namespace Fcg.Payment.Domain.Payments
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        Task<Payment?> GetByPspReferenceAsync(string pspRef, CancellationToken cancellationToken);

        /// <summary>
        /// Obtém o pagamento pelo ID, incluindo os itens associados.
        /// </summary>
        Task<Domain.Payments.Payment?> GetByPaymentIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
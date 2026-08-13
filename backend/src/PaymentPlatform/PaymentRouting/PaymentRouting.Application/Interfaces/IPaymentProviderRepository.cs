using PaymentRouting.Domain.Providers;

namespace PaymentRouting.Application.Interfaces;

public interface IPaymentProviderRepository
{
    Task AddAsync(
        PaymentProvider provider,
        CancellationToken cancellationToken);

    Task<PaymentProvider?> GetAsync(
        string providerId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PaymentProvider>>
        ListAsync(
            CancellationToken cancellationToken);
}

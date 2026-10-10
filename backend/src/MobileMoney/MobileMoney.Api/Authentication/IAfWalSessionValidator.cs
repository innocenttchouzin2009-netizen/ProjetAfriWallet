namespace MobileMoney.Production.Authentication;

public interface IAfWalSessionValidator
{
    Task<AfWalSessionValidationResult> ValidateAsync(
        string? authorizationHeader,
        CancellationToken cancellationToken = default);
}

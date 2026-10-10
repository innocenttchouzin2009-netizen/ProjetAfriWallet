namespace MobileMoney.Production.Auth;

public interface IAfWalSessionValidator
{
    Task<AfWalSessionValidationResult> ValidateAsync(
        string accessToken,
        Guid userId,
        Guid sessionId,
        long tokenVersion,
        CancellationToken cancellationToken = default);
}

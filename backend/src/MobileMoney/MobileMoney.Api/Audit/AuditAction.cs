namespace MobileMoney.Production.Audit;

public enum AuditAction
{
    ConfigurationChanged,
    SecretProviderInitialized,
    FeatureFlagChanged,
    MtnDepositRequested,
    MtnDepositCompleted,
    MtnWithdrawalRequested,
    MtnWithdrawalCompleted,
    StatusRequested,
    CallbackReceived,
    CallbackValidated,
    CallbackRejected,
    RateLimitExceeded,
    CircuitBreakerOpened,
    InvalidSignature,
    InvalidCallback,
    UnauthorizedRequest,
    HealthCheckExecuted,
    ConfigurationValidated,
    ProductionModeEnabled,
    ProductionModeDisabled
}

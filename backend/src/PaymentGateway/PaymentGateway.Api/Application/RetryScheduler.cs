namespace PaymentGateway.Api.Application;

public sealed class RetryScheduler
{
    public int GetNextDelayMs(int retryCount)
    {
        return retryCount switch
        {
            0 => 100,
            1 => 250,
            _ => 500
        };
    }
}

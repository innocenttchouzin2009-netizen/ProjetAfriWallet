namespace MerchantAcquiring.Application.Services;

public sealed class AcquiringFeeCalculator
{
    public long Calculate(
        long amountMinor,
        decimal percentageFee,
        long fixedFeeMinor)
    {
        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amountMinor));

        var variableFee =
            decimal.Round(
                amountMinor *
                (percentageFee / 100m),
                0,
                MidpointRounding.AwayFromZero);

        return checked(
            (long)variableFee +
            fixedFeeMinor);
    }
}

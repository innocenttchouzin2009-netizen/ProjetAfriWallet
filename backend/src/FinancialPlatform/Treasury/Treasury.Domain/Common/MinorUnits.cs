namespace Treasury.Domain.Common;

public static class MinorUnits
{
    public static long EnsurePositive(long amountMinor)
    {
        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor), "Amount must be greater than zero in minor units.");

        return amountMinor;
    }
}

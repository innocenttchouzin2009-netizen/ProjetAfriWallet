using MobileMoney.Production.Payout.BeneficiaryLookup.Domain;

namespace MobileMoney.Production.Payout.BeneficiaryLookup.Application;

public sealed class CameroonOperatorResolver
{
    public CameroonOperatorResolution Resolve(string phoneNumber)
    {
        var normalized = CameroonPhoneNumberNormalizer.Normalize(phoneNumber);
        var nationalNumber = normalized[4..];

        return new CameroonOperatorResolution(
            normalized,
            ResolveOperator(nationalNumber));
    }

    private static CameroonMobileOperator? ResolveOperator(string nationalNumber)
    {
        if (nationalNumber.StartsWith("67", StringComparison.Ordinal))
            return CameroonMobileOperator.Mtn;

        if (nationalNumber.StartsWith("69", StringComparison.Ordinal))
            return CameroonMobileOperator.Orange;

        var threeDigitPrefix = nationalNumber[..3];

        if (threeDigitPrefix is "650" or "651" or "652" or "653" or "654")
            return CameroonMobileOperator.Mtn;

        if (threeDigitPrefix is "655" or "656" or "657" or "658" or "659")
            return CameroonMobileOperator.Orange;

        return null;
    }
}

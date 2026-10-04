namespace MobileMoney.Production.BeneficiaryResolution.Domain;

public static class BeneficiaryResolutionErrorCode
{
    public const string InvalidPhoneNumber = "BENEFICIARY_RESOLUTION_INVALID_PHONE_NUMBER";
    public const string UnsupportedCountry = "BENEFICIARY_RESOLUTION_UNSUPPORTED_COUNTRY";
    public const string UnsupportedOperator = "BENEFICIARY_RESOLUTION_UNSUPPORTED_OPERATOR";
    public const string NotFound = "BENEFICIARY_RESOLUTION_NOT_FOUND";
    public const string OperatorConfirmationRequired = "BENEFICIARY_RESOLUTION_OPERATOR_CONFIRMATION_REQUIRED";
}

namespace MobileMoney.Production.Authentication;

public enum AfWalSessionValidationStatus
{
    Valid = 0,
    MissingToken = 1,
    Invalid = 2,
    Expired = 3,
    Incompatible = 4,
    Unavailable = 5
}

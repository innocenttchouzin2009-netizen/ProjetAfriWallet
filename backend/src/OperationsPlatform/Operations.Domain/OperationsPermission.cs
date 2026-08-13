namespace Operations.Domain;

public sealed class OperationsPermission
{
    public OperationsRole Role { get; set; }
    public HashSet<OperationsAction> AllowedActions { get; set; } = new();
    public bool RequiresMfa { get; set; }
    public bool RequiresDeviceTrust { get; set; }
}

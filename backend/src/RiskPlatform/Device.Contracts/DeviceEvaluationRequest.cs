namespace Device.Contracts;

public sealed class DeviceEvaluationRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceFingerprint { get; set; } = string.Empty;
    public int DeviceTrustScore { get; set; }
    public int DeviceReputationScore { get; set; }
    public int IpReputationScore { get; set; }
    public bool KnownDevice { get; set; }
    public bool NewDevice { get; set; }
    public string? UsualCountry { get; set; }
    public string? CurrentCountry { get; set; }
    public string? UsualTimezone { get; set; }
    public string? CurrentTimezone { get; set; }
    public bool IsVpn { get; set; }
    public bool IsProxy { get; set; }
    public bool IsTor { get; set; }
    public bool IsImpossibleTravel { get; set; }
    public bool BrowserChanged { get; set; }
    public bool OsChanged { get; set; }
    public bool AppVersionChanged { get; set; }
    public bool TimezoneCountryMismatch { get; set; }
    public bool BiometricAvailable { get; set; }
    public int SessionBehaviorScore { get; set; }
    public int DeviceHistoryCount { get; set; }
    public IDictionary<string, object> Signals { get; set; } = new Dictionary<string, object>();
}

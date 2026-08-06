using Device.Application;
using Device.Contracts;

var engine = new DeviceEngine();

var fingerprintCase = engine.Evaluate(new DeviceEvaluationRequest
{
    DeviceId = "device-001",
    DeviceFingerprint = "fp-known",
    DeviceTrustScore = 90,
    DeviceReputationScore = 85,
    IpReputationScore = 80,
    KnownDevice = true,
    NewDevice = false,
    UsualCountry = "CI",
    CurrentCountry = "CI",
    UsualTimezone = "GMT",
    CurrentTimezone = "GMT",
    IsVpn = false,
    IsProxy = false,
    IsTor = false,
    IsImpossibleTravel = false,
    BrowserChanged = false,
    OsChanged = false,
    AppVersionChanged = false,
    TimezoneCountryMismatch = false,
    BiometricAvailable = true,
    SessionBehaviorScore = 80,
    DeviceHistoryCount = 2
});
if (fingerprintCase.Decision != DeviceDecisionType.Trusted) throw new Exception("device fingerprint failed");

var knownDeviceCase = engine.Evaluate(new DeviceEvaluationRequest
{
    DeviceId = "device-002",
    DeviceFingerprint = "fp-known-2",
    DeviceTrustScore = 85,
    DeviceReputationScore = 82,
    IpReputationScore = 78,
    KnownDevice = true,
    NewDevice = false,
    UsualCountry = "CI",
    CurrentCountry = "CI",
    UsualTimezone = "GMT",
    CurrentTimezone = "GMT",
    IsVpn = false,
    IsProxy = false,
    IsTor = false,
    IsImpossibleTravel = false,
    BrowserChanged = false,
    OsChanged = false,
    AppVersionChanged = false,
    TimezoneCountryMismatch = false,
    BiometricAvailable = true,
    SessionBehaviorScore = 78,
    DeviceHistoryCount = 4
});
if (knownDeviceCase.Decision != DeviceDecisionType.Trusted) throw new Exception("known device failed");

var newDeviceCase = engine.Evaluate(new DeviceEvaluationRequest
{
    DeviceId = "device-003",
    DeviceFingerprint = "fp-new",
    DeviceTrustScore = 55,
    DeviceReputationScore = 70,
    IpReputationScore = 75,
    KnownDevice = false,
    NewDevice = true,
    UsualCountry = "CI",
    CurrentCountry = "CI",
    UsualTimezone = "GMT",
    CurrentTimezone = "GMT",
    IsVpn = false,
    IsProxy = false,
    IsTor = false,
    IsImpossibleTravel = false,
    BrowserChanged = false,
    OsChanged = false,
    AppVersionChanged = false,
    TimezoneCountryMismatch = false,
    BiometricAvailable = false,
    SessionBehaviorScore = 70,
    DeviceHistoryCount = 1
});
if (newDeviceCase.Decision != DeviceDecisionType.Suspicious) throw new Exception("new device failed");

var trustCase = engine.Evaluate(new DeviceEvaluationRequest
{
    DeviceId = "device-004",
    DeviceFingerprint = "fp-trust",
    DeviceTrustScore = 20,
    DeviceReputationScore = 70,
    IpReputationScore = 75,
    KnownDevice = true,
    NewDevice = false,
    UsualCountry = "CI",
    CurrentCountry = "CI",
    UsualTimezone = "GMT",
    CurrentTimezone = "GMT",
    IsVpn = false,
    IsProxy = false,
    IsTor = false,
    IsImpossibleTravel = false,
    BrowserChanged = false,
    OsChanged = false,
    AppVersionChanged = false,
    TimezoneCountryMismatch = false,
    BiometricAvailable = true,
    SessionBehaviorScore = 75,
    DeviceHistoryCount = 2
});
if (trustCase.Decision != DeviceDecisionType.Suspicious) throw new Exception("device trust score failed");

var travelCase = engine.Evaluate(new DeviceEvaluationRequest
{
    DeviceId = "device-005",
    DeviceFingerprint = "fp-travel",
    DeviceTrustScore = 70,
    DeviceReputationScore = 72,
    IpReputationScore = 76,
    KnownDevice = true,
    NewDevice = false,
    UsualCountry = "CI",
    CurrentCountry = "FR",
    UsualTimezone = "GMT",
    CurrentTimezone = "CET",
    IsVpn = false,
    IsProxy = false,
    IsTor = false,
    IsImpossibleTravel = true,
    BrowserChanged = false,
    OsChanged = false,
    AppVersionChanged = false,
    TimezoneCountryMismatch = false,
    BiometricAvailable = true,
    SessionBehaviorScore = 70,
    DeviceHistoryCount = 3
});
if (travelCase.Decision != DeviceDecisionType.HighRisk) throw new Exception("impossible travel failed");

var vpnCase = engine.Evaluate(new DeviceEvaluationRequest
{
    DeviceId = "device-006",
    DeviceFingerprint = "fp-vpn",
    DeviceTrustScore = 70,
    DeviceReputationScore = 72,
    IpReputationScore = 76,
    KnownDevice = true,
    NewDevice = false,
    UsualCountry = "CI",
    CurrentCountry = "CI",
    UsualTimezone = "GMT",
    CurrentTimezone = "GMT",
    IsVpn = true,
    IsProxy = false,
    IsTor = false,
    IsImpossibleTravel = false,
    BrowserChanged = false,
    OsChanged = false,
    AppVersionChanged = false,
    TimezoneCountryMismatch = false,
    BiometricAvailable = true,
    SessionBehaviorScore = 70,
    DeviceHistoryCount = 3
});
if (vpnCase.Decision != DeviceDecisionType.Suspicious) throw new Exception("vpn detection failed");

var proxyCase = engine.Evaluate(new DeviceEvaluationRequest
{
    DeviceId = "device-007",
    DeviceFingerprint = "fp-proxy",
    DeviceTrustScore = 70,
    DeviceReputationScore = 72,
    IpReputationScore = 76,
    KnownDevice = true,
    NewDevice = false,
    UsualCountry = "CI",
    CurrentCountry = "CI",
    UsualTimezone = "GMT",
    CurrentTimezone = "GMT",
    IsVpn = false,
    IsProxy = true,
    IsTor = false,
    IsImpossibleTravel = false,
    BrowserChanged = false,
    OsChanged = false,
    AppVersionChanged = false,
    TimezoneCountryMismatch = false,
    BiometricAvailable = true,
    SessionBehaviorScore = 70,
    DeviceHistoryCount = 3
});
if (proxyCase.Decision != DeviceDecisionType.Suspicious) throw new Exception("proxy detection failed");

var anomalyCase = engine.Evaluate(new DeviceEvaluationRequest
{
    DeviceId = "device-008",
    DeviceFingerprint = "fp-anomaly",
    DeviceTrustScore = 80,
    DeviceReputationScore = 72,
    IpReputationScore = 76,
    KnownDevice = true,
    NewDevice = false,
    UsualCountry = "CI",
    CurrentCountry = "CI",
    UsualTimezone = "GMT",
    CurrentTimezone = "GMT",
    IsVpn = false,
    IsProxy = false,
    IsTor = false,
    IsImpossibleTravel = false,
    BrowserChanged = true,
    OsChanged = true,
    AppVersionChanged = true,
    TimezoneCountryMismatch = true,
    BiometricAvailable = false,
    SessionBehaviorScore = 20,
    DeviceHistoryCount = 3
});
if (anomalyCase.Decision != DeviceDecisionType.HighRisk) throw new Exception("behavior anomaly failed");

var trustedCase = engine.Evaluate(new DeviceEvaluationRequest
{
    DeviceId = "device-009",
    DeviceFingerprint = "fp-trusted",
    DeviceTrustScore = 90,
    DeviceReputationScore = 90,
    IpReputationScore = 90,
    KnownDevice = true,
    NewDevice = false,
    UsualCountry = "CI",
    CurrentCountry = "CI",
    UsualTimezone = "GMT",
    CurrentTimezone = "GMT",
    IsVpn = false,
    IsProxy = false,
    IsTor = false,
    IsImpossibleTravel = false,
    BrowserChanged = false,
    OsChanged = false,
    AppVersionChanged = false,
    TimezoneCountryMismatch = false,
    BiometricAvailable = true,
    SessionBehaviorScore = 80,
    DeviceHistoryCount = 2
});
if (trustedCase.Decision != DeviceDecisionType.Trusted) throw new Exception("trusted decision failed");

var highRiskCase = engine.Evaluate(new DeviceEvaluationRequest
{
    DeviceId = "device-010",
    DeviceFingerprint = "fp-high-risk",
    DeviceTrustScore = 40,
    DeviceReputationScore = 40,
    IpReputationScore = 35,
    KnownDevice = true,
    NewDevice = false,
    UsualCountry = "CI",
    CurrentCountry = "CI",
    UsualTimezone = "GMT",
    CurrentTimezone = "GMT",
    IsVpn = false,
    IsProxy = false,
    IsTor = false,
    IsImpossibleTravel = false,
    BrowserChanged = false,
    OsChanged = false,
    AppVersionChanged = false,
    TimezoneCountryMismatch = false,
    BiometricAvailable = false,
    SessionBehaviorScore = 40,
    DeviceHistoryCount = 2
});
if (highRiskCase.Decision != DeviceDecisionType.HighRisk) throw new Exception("high-risk decision failed");

var compromisedCase = engine.Evaluate(new DeviceEvaluationRequest
{
    DeviceId = "device-011",
    DeviceFingerprint = "fp-compromised",
    DeviceTrustScore = 10,
    DeviceReputationScore = 10,
    IpReputationScore = 10,
    KnownDevice = false,
    NewDevice = true,
    UsualCountry = "CI",
    CurrentCountry = "DE",
    UsualTimezone = "GMT",
    CurrentTimezone = "CET",
    IsVpn = true,
    IsProxy = true,
    IsTor = true,
    IsImpossibleTravel = true,
    BrowserChanged = true,
    OsChanged = true,
    AppVersionChanged = true,
    TimezoneCountryMismatch = true,
    BiometricAvailable = false,
    SessionBehaviorScore = 10,
    DeviceHistoryCount = 5
});
if (compromisedCase.Decision != DeviceDecisionType.Compromised) throw new Exception("compromised decision failed");

if (fingerprintCase.AuditEvents.Count < 1 || compromisedCase.AuditEvents.Count < 1) throw new Exception("audit generation failed");
if (compromisedCase.Telemetry == null || compromisedCase.Telemetry.Score <= 0) throw new Exception("telemetry generation failed");

Console.WriteLine("device fingerprint ................. PASS");
Console.WriteLine("known device ....................... PASS");
Console.WriteLine("new device ......................... PASS");
Console.WriteLine("device trust score ................. PASS");
Console.WriteLine("impossible travel ................. PASS");
Console.WriteLine("vpn detection ...................... PASS");
Console.WriteLine("proxy detection .................... PASS");
Console.WriteLine("behavior anomaly ................... PASS");
Console.WriteLine("trusted decision ................... PASS");
Console.WriteLine("high-risk decision ................. PASS");
Console.WriteLine("compromised decision ............... PASS");
Console.WriteLine("audit generation ................... PASS");
Console.WriteLine("telemetry generation ............... PASS");
Console.WriteLine();
Console.WriteLine("All AFW-DLV-0011.4 device intelligence scenarios passed.");

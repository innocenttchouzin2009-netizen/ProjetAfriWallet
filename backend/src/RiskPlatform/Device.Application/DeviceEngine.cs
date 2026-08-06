using Device.Contracts;

namespace Device.Application;

public sealed class DeviceEngine
{
    public DeviceEvaluationResult Evaluate(DeviceEvaluationRequest request)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var triggeredSignals = new List<string>();
        var auditEvents = new List<string> { "DEVICE_EVALUATION_STARTED" };
        var score = 0;

        if (!string.IsNullOrWhiteSpace(request.DeviceFingerprint))
        {
            triggeredSignals.Add("device-fingerprint");
            score += 15;
        }

        if (request.KnownDevice)
        {
            triggeredSignals.Add("known-device");
            score += 0;
        }
        else if (request.NewDevice)
        {
            triggeredSignals.Add("new-device");
            score += 20;
        }

        if (request.DeviceTrustScore <= 40)
        {
            triggeredSignals.Add("device-trust-score");
            score += 20;
        }

        if (request.IsImpossibleTravel)
        {
            triggeredSignals.Add("impossible-travel");
            score += 40;
        }

        if (request.IsVpn || request.IsProxy || request.IsTor)
        {
            triggeredSignals.Add("network-anonymity");
            score += 20;
        }

        if (request.BrowserChanged || request.OsChanged || request.AppVersionChanged)
        {
            triggeredSignals.Add("environment-change");
            score += 15;
        }

        if (request.TimezoneCountryMismatch)
        {
            triggeredSignals.Add("timezone-country-mismatch");
            score += 10;
        }

        if (request.BiometricAvailable)
        {
            triggeredSignals.Add("biometric-available");
            score += 0;
        }

        if (request.SessionBehaviorScore < 40)
        {
            triggeredSignals.Add("behavior-anomaly");
            score += 20;
        }

        if (request.DeviceHistoryCount > 3)
        {
            triggeredSignals.Add("device-history");
            score += 0;
        }

        if (request.IpReputationScore < 40)
        {
            triggeredSignals.Add("ip-reputation");
            score += 15;
        }

        if (request.DeviceReputationScore < 40)
        {
            triggeredSignals.Add("device-reputation");
            score += 15;
        }

        var decision = score >= 85 ? DeviceDecisionType.Compromised : score >= 50 ? DeviceDecisionType.HighRisk : score >= 30 ? DeviceDecisionType.Suspicious : DeviceDecisionType.Trusted;
        var riskLevel = decision switch
        {
            DeviceDecisionType.Compromised => "CRITICAL",
            DeviceDecisionType.HighRisk => "HIGH",
            DeviceDecisionType.Suspicious => "MEDIUM",
            _ => "LOW"
        };

        auditEvents.Add(decision switch
        {
            DeviceDecisionType.Compromised => "DEVICE_COMPROMISED",
            DeviceDecisionType.HighRisk => "DEVICE_HIGH_RISK",
            DeviceDecisionType.Suspicious => "DEVICE_SUSPICIOUS",
            _ => "DEVICE_TRUSTED"
        });

        var durationMs = (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds;
        var telemetry = new DeviceTelemetry
        {
            Decision = decision.ToString().ToUpperInvariant(),
            RiskLevel = riskLevel,
            Score = score,
            TriggeredSignalCount = triggeredSignals.Count,
            EvaluationDurationMs = durationMs
        };

        return new DeviceEvaluationResult
        {
            Decision = decision,
            RiskLevel = riskLevel,
            Score = score,
            TriggeredSignals = triggeredSignals,
            AuditEvents = auditEvents,
            Telemetry = telemetry,
            DeviceFingerprint = request.DeviceFingerprint,
            KnownDevice = request.KnownDevice,
            NewDevice = request.NewDevice,
            DeviceTrustScore = request.DeviceTrustScore
        };
    }
}

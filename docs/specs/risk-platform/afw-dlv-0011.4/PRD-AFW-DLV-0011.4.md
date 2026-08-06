# PRD AFW-DLV-0011.4 — Device Intelligence & Behavioral Analytics

## Summary
Implement a device-intelligence capability for AfriWallet that evaluates device trust, behavioral signals, and risk posture for digital identity and transaction flows.

## Goals
- Evaluate device trust using fingerprint, reputation, IP reputation, history, and behavioral signals.
- Return explainable decisions: Trusted, Suspicious, HighRisk, or Compromised.
- Emit audit events and telemetry for every evaluation.
- Provide an HTTP API for device evaluation and trust lifecycle operations.

## Scope
- POST /api/v1/device/evaluate
- GET /api/v1/device/{deviceId}
- GET /api/v1/device/{deviceId}/history
- POST /api/v1/device/{deviceId}/trust
- POST /api/v1/device/{deviceId}/revoke

## Success Criteria
- Scenario harness passes for trusted, suspicious, high-risk, and compromised decisions.
- Build succeeds for Device.Api in Release mode.
- Documentation and validation artifacts are generated in docs/specs/risk-platform/afw-dlv-0011.4.

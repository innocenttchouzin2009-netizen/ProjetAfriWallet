# ADR-0125 — Sensitive Telemetry Data Policy

## Status
Accepted

## Context
Telemetry must not leak tokens, keys, secrets, full phone numbers, raw request bodies, or other sensitive data.

## Decision
Only a controlled set of safe attributes may be recorded on activities, and diagnostics must expose only exporter, service, version, environment, sources, and meter metadata.

## Consequences
- Telemetry remains compliant with security requirements.
- Sensitive values are not exposed to logs, traces, or diagnostics.

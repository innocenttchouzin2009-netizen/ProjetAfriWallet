# ADR-0262 - Financial Risk Scoring

## Status

Accepted for AFW-DLV-0016.5.

## Decision

Risk scoring aggregates normalized KYC, sanctions/PEP and AML monitoring signals through provider interfaces. Source engines remain independent bounded capabilities.

## Calculation

Each raw signal is clamped to 0-100, multiplied by its configured weight and divided by total active weight. The result is clamped to 0-100.

## Boundary

The output is an explainable internal operational control, not a legal determination or regulatory finding.
# Runbook — AFW-DLV-0011.4

## Validation steps
1. Run the scenario harness.
2. Build the API in Release mode.
3. Review the emitted decision and telemetry response for each evaluation.

## Common issues
- If the decision is too permissive, revisit the trust-score and reputation weights.
- If the decision is too aggressive, reduce the network-anonymity and environment-change penalties.

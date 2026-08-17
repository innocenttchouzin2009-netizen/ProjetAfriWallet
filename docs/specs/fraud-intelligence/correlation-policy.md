# Fraud Intelligence Correlation Policy - Sandbox

- Shared device: detect when the subject AWID and another AWID use the same device.
- Shared beneficiary: detect beneficiary convergence across distinct AWIDs.
- Repeated high-risk transactions: trigger at least two transactions with fraud score >= 60.
- Repeated fraud cases: trigger at least two cases for the subject.
- Compound risk: trigger when at least three independent patterns are present.

Pattern scores are additive and capped at 100. Severity thresholds are 0-9 informational, 10-29 low, 30-59 medium, 60-79 high, and 80-100 critical. These are sandbox policy values, not production calibration.
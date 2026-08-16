# AFW-DLV-0017.2 - Device & Account Risk Engine

Produces explainable device/account risk profiles from canonical 0017.1 snapshots. Factors cover new/changed devices, login/PIN failures, multiple devices, velocity, session anomaly, and account-device mismatch. Bands are LOW/MEDIUM/HIGH/CRITICAL; recommendations are ALLOW/REVIEW/CHALLENGE/RESTRICT.

Recommendations are non-executing. This delivery blocks no payment, suspends no account, revokes no device, executes no MFA, and duplicates neither Identity nor Compliance.
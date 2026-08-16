# AFW-DLV-0016.4 Security Boundary

## Stored

- normalized transaction metadata
- rule evaluations returned to the caller
- risk score
- alert metadata
- audit events

## Not provided by this delivery

- production regulatory thresholds
- SAR/STR filing credentials
- regulator API credentials
- external AML datasets
- regulatory certification

Monitoring logic consumes only transaction attributes required to evaluate configured rules.
# Fraud Response Policy - Sandbox

- Fraud score 0-29: `NO_ACTION`
- Fraud score 30-59: `MONITOR`
- Fraud score 60-79: `CHALLENGE_CUSTOMER`
- Fraud score 80-100: `DECLINE_RECOMMENDED`

Analysts may also record `ACCOUNT_RESTRICTION_RECOMMENDED` and `DEVICE_REVOCATION_RECOMMENDED`. These values do not execute account, payment, or device mutations.
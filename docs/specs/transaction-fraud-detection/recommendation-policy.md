# Transaction Fraud Recommendation Policy

0–29: ALLOW
30–59: REVIEW
60–79: CHALLENGE
80–100: DECLINE_RECOMMENDED

## Execution boundary
DECLINE_RECOMMENDED does not change payment state.
Execution requires a separate decision/orchestration delivery.

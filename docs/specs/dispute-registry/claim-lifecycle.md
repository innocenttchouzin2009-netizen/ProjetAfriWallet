# Claim Lifecycle - Sandbox

Allowed transitions:

- `DRAFT -> SUBMITTED`
- `SUBMITTED -> OPEN`
- `OPEN -> UNDER_REVIEW`
- `UNDER_REVIEW -> RESOLVED`
- `RESOLVED -> CLOSED`
- `SUBMITTED | OPEN | UNDER_REVIEW -> REJECTED`
- `DRAFT | SUBMITTED | OPEN -> CANCELLED`

Any other transition is rejected. Resolution requires a recorded outcome; rejection and cancellation require a reason.

Claim amounts must be positive and expressed in minor units with a three-letter currency code.

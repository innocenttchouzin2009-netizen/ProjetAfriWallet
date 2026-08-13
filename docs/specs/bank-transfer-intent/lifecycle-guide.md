# Bank Transfer Intent Lifecycle Guide

1. Create a transfer intent with a unique idempotency key.
2. Validate beneficiary existence, activity, verification, and currency consistency.
3. Confirm the intent once the customer has approved the request.
4. Mark the intent ready for routing when the platform is prepared to hand off to the routing layer.
5. Cancel only while the intent is not finalized.
6. Expiration is enforced at the domain boundary before transitions.

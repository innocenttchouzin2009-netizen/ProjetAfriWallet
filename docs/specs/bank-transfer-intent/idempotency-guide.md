# Idempotency Guide

- Every submission must include a stable idempotency key.
- The repository resolves a duplicate key to the original transfer intent.
- Repeated requests with the same key do not create additional intents.
- This prevents duplicate customer submissions at the intent layer.

# Sandbox / Production Boundary

AFW-DLV-0015.7 validates Banking Platform architecture and operations.
It does not authorize production banking traffic.

The following remain sandbox:
- bank provider adapters
- provider credentials
- SEPA simulations
- SWIFT simulations
- local bank simulations
- webhook secrets used by tests

Production activation requires a separate controlled delivery.

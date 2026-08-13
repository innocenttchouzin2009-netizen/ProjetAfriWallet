# Payment Platform Configuration Matrix

| Area | Sandbox | Production |
| --- | --- | --- |
| Provider connectors | Included sandbox adapters | Separate certified adapters required |
| Credentials | Synthetic short-lived values | Approved secret store only |
| Webhook secrets | Runtime scenario value | Environment-specific secret reference |
| Provider endpoints | No real operator calls | HTTPS provider endpoints after approval |
| Retry and timeout | Bounded defaults | Provider-approved values |
| Feature activation | Sandbox composition | Explicit controlled enablement |

No secret value belongs in this file or any other release artifact.
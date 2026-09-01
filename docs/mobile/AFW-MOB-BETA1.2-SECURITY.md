# Authentication Security Boundary

AFW-MOB-BETA1.2 provides presentation and local validation only.

The Flutter client MUST NOT become the source of truth for authentication. A contact/PIN form passing local validation is not proof of identity and does not create an authenticated financial session.

Production activation requires validated AfWal backend flows for OTP, PIN verification, trusted-device controls, session issuance/rotation and audit. Secrets and raw PIN values must not be persisted by this delivery.

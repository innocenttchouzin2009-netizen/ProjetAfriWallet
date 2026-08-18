# Merchant Payment Authorization Policy - Sandbox v1

Preconditions: Payment Intent `ReadyForAuthorization`, Merchant Registry `Active`, Merchant Verification `Verified`.

Risk bands: `0-59` Authorize, `60-84` RequiresStepUp, `85-100` Decline. Maximum amount is `1,000,000` minor units; higher amounts decline. Expired intents decline. An approved Authorize decision may become CaptureEligible without executing capture. These are sandbox policy rules.

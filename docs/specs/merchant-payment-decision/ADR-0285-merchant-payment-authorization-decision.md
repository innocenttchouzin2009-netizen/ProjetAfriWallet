# ADR-0285 - Merchant Payment Authorization Decision

`0019.3` creates commerce intent; `0019.4` decides policy acceptability; a later delivery may execute real authorization/capture. Risk is an input snapshot, not a duplicated Fraud Platform. Step-up completion changes a decision to `Authorize`; `CaptureEligible` remains a decision. No Universal Ledger writer is introduced.

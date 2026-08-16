# AFW-DLV-0017.1 - Fraud Signal & Event Platform

Provides canonical ingestion, normalization, source/type classification, generic subject association, occurrence timestamps, metadata preservation, event deduplication, subject history, and audit evidence.

Supported source domains are Payment, Wallet, Device, Identity, Beneficiary, Bank Transfer, Merchant, Compliance, and Authentication.

This delivery calculates no fraud score, blocks no payment, freezes no wallet, suspends no user, duplicates no AML/KYC or sanctions engine, makes no legal conclusion, and submits no regulatory report.

Compliance events remain source evidence only: AML suspicion is not automatically fraud, and fraud evidence is not automatically AML suspicion.

Local success means READY FOR REVIEW. Freeze requires green PR CI, squash merge, exact SHA, and immutable tag parity.
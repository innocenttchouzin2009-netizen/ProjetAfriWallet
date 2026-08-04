# ADR-0101: Supply Chain Security

- Status: Accepted
- Date: 2026-08-04

## Context
Build artifacts, container images, and release assets must be traceable and protected against tampering.

## Decision
The CI/CD pipeline will generate build provenance signals, packaging artifacts, and SBOM placeholders while integrating release signing and verification practices as the platform matures.

## Consequences
AfriWallet establishes a foundation for stronger supply-chain controls without overcommitting to a specific vendor implementation at this stage.

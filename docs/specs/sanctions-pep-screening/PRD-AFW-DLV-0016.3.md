# AFW-DLV-0016.3 - Sanctions & PEP Screening Engine

## Objective

Provide a provider-neutral sanctions and PEP screening engine.

## Responsibilities

- subject normalization
- provider-neutral list sources
- sanctions screening
- PEP screening
- deterministic matching
- score calculation
- decision thresholds
- audit trail
- false-positive workflow foundation
- sandbox enforcement

## Decisions

- Clear
- Review
- Block

## Matching model

The engine combines normalized name similarity, date-of-birth match and country match. The resulting score is deterministic and explainable.

## Security boundary

Raw provider credentials and licensed regulatory datasets are outside the core domain.

## Production boundary

All lists in AFW-DLV-0016.3 are synthetic sandbox data. No external regulatory dataset or regulatory screening certification is claimed.

## Out of scope

- production sanctions data
- production PEP data
- adverse media
- AML transaction monitoring
- regulatory filing
- automatic legal determinations

## Decision

Successful validation means: READY FOR REVIEW

It does not mean:

- SANCTIONS CERTIFIED
- AML CERTIFIED
- REGULATORY APPROVED
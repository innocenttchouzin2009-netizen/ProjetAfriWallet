# ADR-0260 - Sanctions & PEP Screening Engine

## Status

Accepted for AFW-DLV-0016.3.

## Decision

Screening uses provider-neutral list interfaces and deterministic, explainable matching. The compliance core does not depend on a specific sanctions or PEP vendor.

## Source boundary

AFW-DLV-0016.3 accepts sandbox sources only. Production datasets and provider credentials require a separate controlled delivery.

## Matching boundary

Matching produces a screening signal and reasons. It does not make an automatic legal or regulatory determination.

## Evidence

Non-clear matches are retained in the result repository and screening completion is recorded in the audit store.
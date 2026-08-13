# ADR-0229 — Payment Intent Lifecycle and Idempotency

## Status
Accepted

## Context
Une intention de paiement doit garantir la cohérence des transactions et éviter les doublons.

## Decision
- La clé d’idempotence est la clé de réconciliation.
- L’état suit le cycle Created → Authorized → Processing → Completed.
- Toute transition invalide génère une exception métier.

## Consequences
Le système évite les paiements dupliqués et protège la cohérence du flux.

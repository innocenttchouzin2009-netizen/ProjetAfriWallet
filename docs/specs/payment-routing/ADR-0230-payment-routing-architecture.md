# ADR-0230 — Payment Routing Architecture

## Status
Accepted

## Context
Le Payment Intent Engine produit une intention de paiement. Il faut ensuite sélectionner la meilleure route d’exécution.

## Decision
Les providers sont évalués à partir d’un modèle de score combinant coût, fiabilité, latence et priorité. Les décisions sont mémorisées pour garantir l’idempotence.

## Consequences
- Séparation stricte entre intention et exécution.
- Décision centralisée et traçable.
- Extension facile vers de nouveaux connecteurs.

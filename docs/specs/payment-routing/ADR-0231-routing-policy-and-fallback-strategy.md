# ADR-0231 — Routing Policy and Fallback Strategy

## Status
Accepted

## Context
Les canaux de paiement doivent être sélectionnés intelligemment, même lorsqu’un provider est dégradé ou indisponible.

## Decision
La stratégie est fondée sur un score pondéré et sur des routes de secours. Les providers indisponibles sont refusés et les alternatives sont conservées dans la décision.

## Consequences
- meilleure résilience métier
- possibilité de bascule automatique
- traçabilité explicite des alternatives
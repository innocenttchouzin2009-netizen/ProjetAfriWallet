# ADR-0228 — Payment Intent Architecture

## Status
Accepted

## Context
AfriWallet a besoin d’un point d’entrée unique qui encapsule le souhait de paiement avant toute exécution technique.

## Decision
Le système expose un service de création et de suivi d’intentions, avec un repository mémoire pour l’implémentation de référence.

## Consequences
- Séparation claire entre description du paiement et exécution.
- Validation centralisée dans le domaine.
- Une future couche de routage peut s’appuyer sur cette intention sans réécrire les règles métier.

# Idempotency Guide

## Principe
La clé d’idempotence identifie une demande de paiement unique.

## Règles
- Une clé identique doit toujours produire la même intention.
- L’état ne doit pas être recréé en double.
- Les services reposent sur le repository pour éviter les collisions.

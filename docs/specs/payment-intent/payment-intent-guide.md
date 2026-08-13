# Payment Intent Guide

## Rôle
Le Payment Intent Engine décrit l’intention de paiement.

## Règles
- Le montant doit être > 0.
- La devise doit être un code ISO 4217 à 3 lettres.
- Le payer et le payee doivent être distincts.
- La clé d’idempotence doit rester stable pour une même demande.

## Usage
Créer une intention via l’API, puis la valider via les endpoints d’autorisation, traitement et finalisation.

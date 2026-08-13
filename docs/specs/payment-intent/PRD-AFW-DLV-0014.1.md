# PRD AFW-DLV-0014.1 — Payment Intent Engine

## Objectif
La plateforme AfriWallet doit disposer d’un point d’entrée unique pour la création et le suivi des intentions de paiement. Cet engine ne décide pas du mode d’exécution du paiement ; il décrit uniquement le souhait de paiement, les contraintes de validation et son cycle de vie.

## Scope
- Créer une intention de paiement avec validation de montant, devise et identifiants.
- Garantir l’idempotence sur la clé métier.
- Suivre les états Created → Authorized → Processing → Completed.
- Bloquer les transitions invalides.
- Exposer une API minimale pour l’intégration technique.

## Non-scope
- Exécution du transfert.
- Routage de paiement.
- Choix du canal de liquidation.

## Critères d’acceptation
- Une demande invalide est rejetée.
- Deux créations identiques avec la même clé d’idempotence renvoient la même intention.
- Le statut évolue selon un cycle autorisé.
- Les états finaux ne peuvent plus changer.

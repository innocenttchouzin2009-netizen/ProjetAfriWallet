# PRD AFW-DLV-0014.2 — Payment Routing Engine

## Objectif
Cette livraison choisit la meilleure route d’exécution pour une intention de paiement existante. Elle ne crée pas le paiement ; elle décide de la meilleure voie technique selon les contraintes du contexte.

## Scope
- Sélectionner le provider compatible avec la méthode, le pays et la devise.
- Évaluer les providers selon coût, fiabilité, latence et priorité.
- Exclure les providers indisponibles ou désactivés.
- Produire une RoutingDecision avec alternatives.
- Garantir l’idempotence par payment intent.

## Non-scope
- Exécution du transfert.
- Intégration production des providers.
- Requêtes de paiement réelles.

## Providers seedés
Les providers fournis dans ce lot sont des connecteurs sandbox ou logiques de démonstration, et non des intégrations production.

# AFW-DLV-0007.3.4.3 — PRD

## Objet

Mettre en place une journalisation structurée et traçable de bout en bout pour les opérations MTN MoMo, sans exposer de données sensibles.

## Exigences

- Middleware de correlation renforcé avec propagation HTTP
- Validation et génération automatique de CorrelationId
- Logs structurés avec scopes et métadonnées stables
- Redaction des données sensibles et masquage des numéros de téléphone
- Gestion centralisée des erreurs et réponses normalisées
- Format JSON configurable pour Staging et Production

# ADR — AFW-DLV-0007.3.4.2

**Titre :** Separate Health Signals for Liveness, Readiness, and Startup

## Décision

Les vérifications de santé seront exposées via trois endpoints distincts afin de séparer les signaux de disponibilité du processus, de capacité à accepter du trafic et de validité initiale des dépendances critiques.

## Conséquences

- meilleure observabilité opérationnelle
- intégration plus simple avec orchestrateurs et probes
- réduction du risque d'indisponibilité masquée

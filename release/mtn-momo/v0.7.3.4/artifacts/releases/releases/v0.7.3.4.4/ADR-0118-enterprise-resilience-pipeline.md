# ADR-0118 — Enterprise Resilience Pipeline

## Décision

Le backend MobileMoney adopte une pipeline de résilience standardisée basée sur retry, timeout, circuit breaker et fallback contrôlé.

## Conséquence

Les intégrations peuvent être rendues robustes et réutilisables pour plusieurs providers sans dupliquer la logique de résilience.

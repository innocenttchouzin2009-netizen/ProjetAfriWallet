# Fallback Guide

## Principe
Les routes alternatives sont mémorisées dans la décision pour empêcher une failover sans visibilité.

## Règles
- Un provider principal est sélectionné selon sa meilleure note.
- Les alternatives restent en ordre de score décroissant.
- Les chemins non valides sont rejetés avant le routeing.

# ADR-0120 — Partitioned Mobile Money Rate Limiting

## Décision

Le backend MobileMoney applique des politiques de rate limiting partitionnées avec ASP.NET Core Rate Limiting, afin de protéger les endpoints critiques sans restreindre inutilement les health checks.

# ADR-0267 - Device & Account Risk Engine

Consume provider-neutral snapshots rather than referencing the 0017.1 domain. Aggregate deterministic factors over a 24-hour window, persist the latest profile, and audit every calculation. Recommendations never execute controls.
# Explainability

Every protection score is the sum of explicit `ProtectionPattern` contributions. Each pattern records:

- `Code` - a stable, documented pattern identifier (e.g. `DSP-INT-MERCHANT-CONCENTRATION`)
- `Score` - the pattern's contribution to the overall score (0-100)
- `Reason` - a human-readable explanation
- `References` - the claim identifiers that triggered the pattern

No pattern is ever added silently; a `ProtectionFinding` with a non-zero score always carries at least one matching pattern that explains it. This delivery introduces no opaque machine-learning classifier - every contribution is deterministic and traceable to source data from AFW-DLV-0018.1 through 0018.5.

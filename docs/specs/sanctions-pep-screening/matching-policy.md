# Screening Matching Policy

## Principles

The engine must be deterministic, explainable, provider-neutral and auditable.

## Default weights

- name: 70%
- date of birth: 20%
- country: 10%

Name similarity is the maximum token-set Jaccard score across the primary name and aliases after case, accent, punctuation and whitespace normalization.

## Default thresholds

- score below 0.70: Clear
- score at least 0.70: Review
- score at least 0.90: Block

## Important

A score is a screening signal, not a legal conclusion. Production thresholds require compliance approval and validation against approved regulatory data.
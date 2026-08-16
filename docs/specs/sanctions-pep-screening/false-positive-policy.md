# False Positive Policy

A potential sanctions or PEP match must remain reviewable. The engine must not silently delete historical screening matches.

A reviewed match may later be classified as:

- ConfirmedMatch
- FalsePositive

The original screening evidence remains auditable. Production false-positive decisions require an authorized compliance actor.
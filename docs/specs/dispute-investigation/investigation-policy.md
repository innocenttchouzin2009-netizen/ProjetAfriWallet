# Investigation Policy - Sandbox

| Rule | Behavior |
|---|---|
| Assignment | An investigation must be `Open` before an analyst can be assigned. |
| Evidence requests | Require an assigned analyst; allowed while `Assigned` or `WaitingForEvidence`. |
| Evidence submission | Requires an assigned analyst; duplicate SHA-256 hashes are rejected. |
| Auto-fulfillment | Submitting evidence automatically fulfills the first open request of the matching `EvidenceType`. |
| Auto-advance | The investigation moves to `UnderReview` once no open evidence requests remain. |
| Completion | Requires a non-`None` outcome, an assigned analyst, and no open evidence requests. |
| Closure | Requires the investigation to be `Completed`. |
| Immutability | `Completed` and `Closed` investigations reject further assignment, evidence requests, and evidence submission. |

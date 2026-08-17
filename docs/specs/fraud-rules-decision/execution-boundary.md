# Fraud Decision Execution Boundary

AFW-DLV-0017.4 produces decisions only. It must not directly call payment execution, bank transfer execution, wallet state mutation, device revocation, or account suspension.

`ALLOW`, `REVIEW`, `CHALLENGE`, and `DECLINE_RECOMMENDED` are outputs, not commands. A future execution/orchestration component requires separate governance.
# Execution Boundary

AFW-DLV-0018.3 produces evidence and investigation state only.

It must not approve or execute refunds, initiate chargebacks, recover merchant funds, reverse ledger entries, or move money.

An investigation `Outcome` (e.g. `EvidenceSupportsClaim`) is an analyst conclusion about the evidence; it is not a refund decision and does not trigger any financial action.

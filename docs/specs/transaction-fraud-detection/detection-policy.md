# Transaction Fraud Detection Policy

## Sandbox factor weights
Unusual amount ................. 25
New beneficiary ................ 20
High transaction velocity ...... 25
Recent device change ........... 25
Device risk .................... 15–35
Failed then success ............ 15
Geographic anomaly ............. 20
Repeated attempts .............. 20

## Bands
0–29   LOW
30–59  MEDIUM
60–79  HIGH
80–100 CRITICAL

## Principle
All detections must be deterministic and explainable.
No opaque ML model is introduced in AFW-DLV-0017.3.

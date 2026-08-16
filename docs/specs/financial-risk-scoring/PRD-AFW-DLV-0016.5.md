# AFW-DLV-0016.5 - Financial Risk Scoring Engine

## Responsibilities

- consume normalized compliance signals
- calculate weighted risk score
- expose individual factor contributions
- classify risk band
- produce operational decision
- preserve explainability
- persist latest risk profile
- emit audit events

## Decisions

- ALLOW
- REVIEW
- RESTRICT

## Important boundary

A financial-risk decision is an internal operational control. It is not a legal determination, regulatory filing or proof of criminal activity.

## No engine duplication

AFW-DLV-0016.5 does not reimplement KYC verification, sanctions matching, PEP matching or AML transaction rules. It consumes normalized results from those capabilities.

## Sandbox policy

Weights and thresholds are sandbox policy values. Production scoring requires compliance governance and calibration.

## Decision

Successful validation means READY FOR REVIEW. It does not mean regulatory approved, AML certified or production risk policy approved.
# Merchant Onboarding Operations Runbook

## Overview
This runbook handles the merchant onboarding and KYC release.

## Health checks
- Confirm the merchant API is reachable and the health endpoint responds.
- Verify onboarding, KYC submission, approval, rejection, and activation requests return expected responses.

## Troubleshooting
- If profile completion fails, validate the required fields before resubmission.
- If KYC review is stuck, confirm the KYC case exists and has a valid status.

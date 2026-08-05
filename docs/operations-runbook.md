# Merchant Registry Operations Runbook

## Overview
This runbook covers operational checks for the AFW-DLV-0009.1 Merchant Registry release.

## Health checks
- Confirm the merchant API health endpoint responds at /health/live.
- Verify that merchant create and lookup requests complete successfully.

## Validation workflow
1. Build the merchant API with Release configuration.
2. Run the merchant registry scenario suite.
3. Review logs for merchant lifecycle transitions and validation failures.

## Incident response
- If merchant creation fails, verify the request payload contains merchantCode, countryCode, baseCurrency, and walletId.
- If lifecycle transitions fail, confirm the merchant exists and is in an eligible status.

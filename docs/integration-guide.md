# Merchant Onboarding Integration Guide

## Overview
This guide describes how downstream services and clients can integrate with the merchant onboarding flow.

## API entry points
- Create onboarding state via /api/v1/merchant-onboarding
- Complete the merchant profile via /api/v1/merchant-onboarding/{merchantId}
- Submit and review the KYC case via /api/v1/merchant-onboarding/{merchantId}/submit and /api/v1/merchant-kyc/{merchantId}

## Notes
- The onboarding domain is intentionally provider-agnostic.
- Future external KYC adapters can be introduced without changing the core onboarding contract.

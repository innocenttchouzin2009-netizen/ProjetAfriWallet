# Release v1.0 Checklist

## Branch Freeze

- [ ] Tag beta (`v1.0.0-beta`)
- [ ] Create `release/v1.0` branch
- [ ] Enable branch protection for release branch
- [ ] Limit changes to fixes and integrations only

## Payments

- [ ] Implement `PaymentProvider` contract
- [ ] Integrate Stripe provider
- [ ] Integrate PayPal provider
- [ ] Persist payment transaction references
- [ ] Implement webhook signature verification
- [ ] Add idempotency for webhook replay
- [ ] Add payment failure recovery states
- [ ] Prepare refund service contract

## Shipping

- [ ] Implement `ShippingProvider` contract
- [ ] Add first carrier implementation
- [ ] Add provider stubs for future carriers
- [ ] Persist tracking numbers on order
- [ ] Trigger shipped notification with tracking

## Billing

- [ ] Create document domain model
- [ ] Generate invoice PDF
- [ ] Generate delivery note PDF
- [ ] Generate quote PDF
- [ ] Generate credit note PDF
- [ ] Attach document list to admin order view

## Stock Advanced

- [ ] Add stock movement ledger
- [ ] Add stock reservations
- [ ] Add inventory sessions
- [ ] Add alert thresholds
- [ ] Add adjustment journal endpoints

## Production Board

- [ ] Add production step definitions
- [ ] Add step execution tracking
- [ ] Add operator validation workflow
- [ ] Add workshop screen for active orders

## Workflow Engine

- [ ] Add workflow definition schema
- [ ] Add configurable transition guards
- [ ] Route order status updates through engine
- [ ] Hook engine events to audit and notifications

## Quality and Go-Live

- [ ] Typecheck and lint pass on release branch
- [x] Integration tests for checkout and webhook flows
- [x] Load test checkout, admin orders, and stock updates
- [x] Error monitoring and alerting configured
- [x] Backup and rollback procedure documented
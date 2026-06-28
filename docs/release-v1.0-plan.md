# Dope&Cute Studio - Release v1.0 Plan

## Objective

Freeze a stable commercial baseline and focus only on production-readiness:

- Real payments (Stripe + PayPal)
- Shipping abstraction with first carrier implementation
- Billing documents (PDF)
- Advanced stock operations
- Production floor workflow
- Configurable workflow engine foundation

## Release Branching Strategy

Target git workflow:

1. Create tag `v1.0.0-beta`
2. Create branch `release/v1.0`
3. Only accept fixes and integration work until GA

Note: if git is unavailable on the environment machine, run these commands once git is installed:

```bash
git tag v1.0.0-beta
git push origin v1.0.0-beta
git checkout -b release/v1.0
```

## Sprint 1 - Real Payments

Scope:

- Stripe checkout/session or payment intents
- PayPal checkout integration
- Payment webhook verification
- Failure handling and retries
- Refund API preparation layer

Implementation shape:

- `PaymentProvider` interface
- `StripePaymentProvider`
- `PaypalPaymentProvider`
- `PaymentService` orchestration
- `PaymentRepository` for persisted payment events/status

Acceptance criteria:

- Successful card and PayPal payment set order to paid/confirmed
- Invalid/failed payment keeps order in pending state
- Duplicate webhook events are idempotent
- Payment audit entries are written

## Sprint 2 - Shipping Module

Scope:

- Carrier abstraction with one active provider first
- Shipment creation and tracking number persistence
- Label/document retrieval hooks

Architecture:

- `ShippingProvider` interface
- Providers: DHL, DPD, UPS, GLS, Colissimo (stubs allowed)
- `ShippingService` with provider routing by config

Acceptance criteria:

- Order can transition to shipped only after shipment creation
- Tracking number is stored and returned by APIs
- Carrier-specific errors are normalized in domain errors

## Sprint 3 - Billing Documents

Scope:

- Invoice PDF
- Delivery note PDF
- Quote PDF
- Credit note PDF

Architecture:

- `DocumentService`
- Template-based generators by document type
- Storage strategy (local/S3-compatible)
- `OrderDocument` entity linked to order

Acceptance criteria:

- Each generated document is versioned and downloadable
- Document metadata is visible in admin order details
- Regeneration does not overwrite historical versions

## Sprint 4 - Advanced Stock

Scope:

- Stock movement history
- Inventory count sessions
- Alert thresholds
- Reservation windows
- Adjustment journal

Architecture:

- `StockMovement` ledger table
- `StockReservation` table
- `InventorySession` and `InventoryLine`
- `StockAlertService`

Acceptance criteria:

- Every stock delta is traceable to one movement row
- Reservations expire safely and release stock
- Negative stock remains impossible under concurrent writes

## Sprint 5 - Production Board

Scope:

- Workshop/production screen per order
- Stage-by-stage completion by operator
- Timestamp and operator traceability

Architecture:

- `ProductionStepDefinition`
- `ProductionStepExecution`
- `ProductionService`

Acceptance criteria:

- Operators can validate steps in sequence
- Admin can view full step timeline per order
- Audit log captures operator and step transitions

## Workflow Engine Foundation

Goal: remove hardcoded order lifecycle transitions.

Core model:

- `WorkflowDefinition` (versioned)
- `WorkflowState`
- `WorkflowTransition`
- `WorkflowInstance`
- `WorkflowInstanceEvent`

Minimal v1 behavior:

- Configured allowed transitions
- Guards for business constraints
- Side effects hooks (notifications, audit)

## Non-Functional Exit Criteria (v1.0)

- Full typecheck and lint green on release branch
- Critical paths covered by integration tests
- Webhook security validation in production mode
- Observability for payment/shipping/document failures
- Rollback and incident runbook available

## Post-v1.0 Parallel Track

Mobile app (React Native or Flutter) can reuse business services through APIs after v1 GA.
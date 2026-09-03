import '../l10n/app_localizations.dart';

String localizeSubscriptionInvoiceStatus(
  AppLocalizations localizations,
  String rawStatus,
) {
  switch (rawStatus.trim().toUpperCase()) {
    case 'DRAFT':
      return localizations.invoiceStatusDraft;
    case 'PENDING':
      return localizations.invoiceStatusPending;
    case 'PAID':
      return localizations.invoiceStatusPaid;
    case 'FAILED':
      return localizations.invoiceStatusFailed;
    case 'OVERDUE':
      return localizations.invoiceStatusOverdue;
    case 'CANCELLED':
    case 'CANCELED':
      return localizations.invoiceStatusCancelled;
    default:
      return rawStatus;
  }
}

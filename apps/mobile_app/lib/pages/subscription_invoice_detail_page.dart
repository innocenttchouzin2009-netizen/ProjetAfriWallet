import 'package:flutter/material.dart';

import '../l10n/app_localizations.dart';
import '../models/subscription_models.dart';
import '../presentation/subscription_invoice_status_presentation.dart';

class SubscriptionInvoiceDetailPage extends StatelessWidget {
  const SubscriptionInvoiceDetailPage({
    super.key,
    required this.invoice,
    this.onReturnToBillingHistory,
  });

  final SubscriptionInvoice invoice;
  final VoidCallback? onReturnToBillingHistory;

  @override
  Widget build(BuildContext context) {
    final localizations = AppLocalizations.of(context)!;
    final statusLabel = localizeSubscriptionInvoiceStatus(localizations, invoice.status);

    return Scaffold(
      appBar: AppBar(
        leading: BackButton(
          key: const Key('subscription-invoice-detail-return-history'),
          onPressed: onReturnToBillingHistory ?? () => Navigator.of(context).pop(),
        ),
        title: Text(localizations.invoiceDetails),
      ),
      body: SafeArea(
        child: ListView(
          key: const Key('subscription-invoice-detail-page'),
          padding: const EdgeInsets.all(16),
          children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _DetailRow(
                      label: localizations.invoiceId,
                      value: invoice.id,
                    ),
                    const Divider(),
                    Padding(
                      padding: const EdgeInsets.symmetric(vertical: 4),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            localizations.invoiceStatus,
                            style: Theme.of(context).textTheme.labelMedium,
                          ),
                          const SizedBox(height: 4),
                          Chip(
                            key: const Key('subscription-invoice-detail-status'),
                            label: Text(statusLabel),
                            visualDensity: VisualDensity.compact,
                          ),
                        ],
                      ),
                    ),
                    const Divider(),
                    _DetailRow(
                      label: localizations.invoiceIssueDate,
                      value: invoice.issueDate,
                    ),
                    const Divider(),
                    _DetailRow(
                      label: localizations.price,
                      value: '${localizations.formatCurrency(invoice.amount)} ${invoice.currency}',
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _DetailRow extends StatelessWidget {
  const _DetailRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: Theme.of(context).textTheme.labelMedium,
          ),
          const SizedBox(height: 4),
          Text(
            value,
            style: Theme.of(context).textTheme.bodyLarge,
          ),
        ],
      ),
    );
  }
}

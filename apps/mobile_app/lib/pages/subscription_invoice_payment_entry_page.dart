import 'package:flutter/material.dart';

import '../l10n/app_localizations.dart';
import '../models/subscription_models.dart';

class SubscriptionInvoicePaymentEntryPage extends StatefulWidget {
  const SubscriptionInvoicePaymentEntryPage({
    super.key,
    required this.invoice,
  });

  final SubscriptionInvoice invoice;

  @override
  State<SubscriptionInvoicePaymentEntryPage> createState() =>
      _SubscriptionInvoicePaymentEntryPageState();
}

class _SubscriptionInvoicePaymentEntryPageState
    extends State<SubscriptionInvoicePaymentEntryPage> {
  String? _selectedMethod;
  bool _showConfirmation = false;

  @override
  Widget build(BuildContext context) {
    final localizations = AppLocalizations.of(context)!;
    final invoice = widget.invoice;

    return Scaffold(
      appBar: AppBar(
        title: Text(localizations.invoicePayment),
      ),
      body: SafeArea(
        child: ListView(
          key: const Key('subscription-invoice-payment-entry-page'),
          padding: const EdgeInsets.all(16),
          children: [
            Text(
              localizations.paymentSummary,
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const SizedBox(height: 12),
            Card(
              key: const Key('subscription-invoice-payment-summary'),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _SummaryRow(
                      label: localizations.invoiceId,
                      value: invoice.id,
                    ),
                    const Divider(),
                    _SummaryRow(
                      label: localizations.price,
                      value:
                          '${localizations.formatCurrency(invoice.amount)} ${invoice.currency}',
                    ),
                    const Divider(),
                    _SummaryRow(
                      label: localizations.invoiceIssueDate,
                      value: invoice.issueDate,
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 24),
            Text(
              localizations.paymentMethod,
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const SizedBox(height: 8),
            Text(localizations.selectPaymentMethod),
            const SizedBox(height: 8),
            RadioListTile<String>(
              key: const Key('invoice-payment-method-wallet'),
              value: 'wallet',
              groupValue: _selectedMethod,
              title: Text(localizations.paymentMethodWallet),
              onChanged: (value) {
                setState(() {
                  _selectedMethod = value;
                  _showConfirmation = false;
                });
              },
            ),
            RadioListTile<String>(
              key: const Key('invoice-payment-method-mobile-money'),
              value: 'mobile-money',
              groupValue: _selectedMethod,
              title: Text(localizations.paymentMethodMobileMoney),
              onChanged: (value) {
                setState(() {
                  _selectedMethod = value;
                  _showConfirmation = false;
                });
              },
            ),
            RadioListTile<String>(
              key: const Key('invoice-payment-method-card'),
              value: 'card',
              groupValue: _selectedMethod,
              title: Text(localizations.paymentMethodCard),
              onChanged: (value) {
                setState(() {
                  _selectedMethod = value;
                  _showConfirmation = false;
                });
              },
            ),
            const SizedBox(height: 16),
            FilledButton(
              key: const Key('invoice-payment-continue'),
              onPressed: _selectedMethod == null
                  ? null
                  : () {
                      setState(() {
                        _showConfirmation = true;
                      });
                    },
              child: Text(localizations.continueToConfirmation),
            ),
            if (_showConfirmation) ...[
              const SizedBox(height: 24),
              Card(
                key: const Key('invoice-payment-confirmation-preview'),
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Text(
                        localizations.confirmInvoicePayment,
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      const SizedBox(height: 12),
                      Text(localizations.confirmPaymentDisclaimer),
                      const SizedBox(height: 16),
                      FilledButton.tonal(
                        key: const Key('invoice-payment-confirm-read-only'),
                        onPressed: () {
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(
                              content: Text(
                                localizations.confirmPaymentDisclaimer,
                              ),
                            ),
                          );
                        },
                        child: Text(localizations.confirmInvoicePayment),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _SummaryRow extends StatelessWidget {
  const _SummaryRow({required this.label, required this.value});

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

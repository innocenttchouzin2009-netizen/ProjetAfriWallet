import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:share_plus/share_plus.dart';

import '../l10n/app_localizations.dart';
import '../models/subscription_models.dart';
import '../services/subscription_invoice_receipt_pdf_service.dart';

enum _PaymentFlowStep {
  method,
  confirmation,
  processing,
  result,
  receipt,
}

typedef ReceiptShareAction = Future<void> Function(String shareText);

class SubscriptionInvoicePaymentEntryPage extends StatefulWidget {
  const SubscriptionInvoicePaymentEntryPage({
    super.key,
    required this.invoice,
    this.simulateFailure = false,
    this.receiptPdfService,
    this.shareReceiptAction,
  });

  final SubscriptionInvoice invoice;
  final bool simulateFailure;
  final SubscriptionInvoiceReceiptPdfService? receiptPdfService;
  final ReceiptShareAction? shareReceiptAction;

  @override
  State<SubscriptionInvoicePaymentEntryPage> createState() =>
      _SubscriptionInvoicePaymentEntryPageState();
}

class _SubscriptionInvoicePaymentEntryPageState
    extends State<SubscriptionInvoicePaymentEntryPage> {
  String? _selectedMethod;
  _PaymentFlowStep _step = _PaymentFlowStep.method;
  bool _isExportingReceipt = false;

  String _paymentMethodLabel(AppLocalizations localizations) {
    switch (_selectedMethod) {
      case 'wallet':
        return localizations.paymentMethodWallet;
      case 'mobile-money':
        return localizations.paymentMethodMobileMoney;
      case 'card':
        return localizations.paymentMethodCard;
      default:
        return '';
    }
  }

  String _receiptShareText(
    AppLocalizations localizations,
    SubscriptionInvoice invoice,
    String paymentReference,
  ) {
    return [
      localizations.paymentReceipt,
      '${localizations.invoiceId}: ${invoice.id}',
      '${localizations.price}: ${localizations.formatCurrency(invoice.amount)} ${invoice.currency}',
      '${localizations.paymentMethod}: ${_paymentMethodLabel(localizations)}',
      '${localizations.paymentReference}: $paymentReference',
      '${localizations.invoiceStatus}: ${localizations.paymentSuccessful}',
    ].join('\n');
  }

  Future<void> _copyPaymentReference(
    String paymentReference,
    AppLocalizations localizations,
  ) async {
    await Clipboard.setData(ClipboardData(text: paymentReference));

    if (!mounted) {
      return;
    }

    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(
            localizations.referenceCopied,
            key: const Key('invoice-payment-receipt-copy-feedback'),
          ),
        ),
      );
  }

  Future<void> _shareReceipt(
    AppLocalizations localizations,
    SubscriptionInvoice invoice,
    String paymentReference,
  ) async {
    final shareText = _receiptShareText(
      localizations,
      invoice,
      paymentReference,
    );

    final action = widget.shareReceiptAction;
    if (action != null) {
      await action(shareText);
      return;
    }

    await SharePlus.instance.share(ShareParams(text: shareText));
  }

  Future<void> _exportReceipt(
    AppLocalizations localizations,
    SubscriptionInvoice invoice,
    String paymentReference,
  ) async {
    if (_isExportingReceipt) {
      return;
    }

    setState(() {
      _isExportingReceipt = true;
    });

    final service =
        widget.receiptPdfService ?? SubscriptionInvoiceReceiptPdfService();

    try {
      final result = await service.export(
        invoiceId: invoice.id,
        data: SubscriptionInvoiceReceiptPdfData(
          brandName: 'AfWal',
          title: localizations.paymentReceiptDocument,
          invoiceLabel: localizations.invoiceId,
          invoiceId: invoice.id,
          amountLabel: localizations.price,
          amount:
              '${localizations.formatCurrency(invoice.amount)} ${invoice.currency}',
          paymentMethodLabel: localizations.paymentMethod,
          paymentMethod: _paymentMethodLabel(localizations),
          paymentReferenceLabel: localizations.paymentReference,
          paymentReference: paymentReference,
          statusLabel: localizations.invoiceStatus,
          status: localizations.paymentSuccessful,
          disclaimer: localizations.confirmPaymentDisclaimer,
        ),
      );

      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            content: Text(
              '${localizations.receiptGenerated}: ${result.fileName}',
              key: const Key('invoice-payment-receipt-download-feedback'),
            ),
          ),
        );
    } catch (_) {
      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            content: Text(
              localizations.receiptGenerationFailed,
              key: const Key('invoice-payment-receipt-download-error'),
            ),
          ),
        );
    } finally {
      if (mounted) {
        setState(() {
          _isExportingReceipt = false;
        });
      }
    }
  }

  Future<void> _confirmPayment() async {
    setState(() {
      _step = _PaymentFlowStep.processing;
    });

    await Future<void>.delayed(const Duration(milliseconds: 250));

    if (!mounted) {
      return;
    }

    setState(() {
      _step = _PaymentFlowStep.result;
    });
  }

  void _retryPayment() {
    setState(() {
      _selectedMethod = null;
      _step = _PaymentFlowStep.method;
    });
  }

  void _returnToInvoices() {
    final navigator = Navigator.of(context);
    if (navigator.canPop()) {
      navigator.pop();
    }
    if (navigator.canPop()) {
      navigator.pop();
    }
  }

  @override
  Widget build(BuildContext context) {
    final localizations = AppLocalizations.of(context)!;
    final invoice = widget.invoice;
    final paymentReference = 'BETA-${invoice.id}';

    return Scaffold(
      appBar: AppBar(
        title: Text(
          _step == _PaymentFlowStep.confirmation
              ? localizations.paymentConfirmation
              : _step == _PaymentFlowStep.result
                  ? localizations.paymentResult
                  : _step == _PaymentFlowStep.receipt
                      ? localizations.paymentReceipt
                      : localizations.invoicePayment,
        ),
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
            if (_step == _PaymentFlowStep.method) ...[
              Text(
                localizations.paymentMethod,
                style: Theme.of(context).textTheme.titleMedium,
              ),
              const SizedBox(height: 8),
              Text(localizations.selectPaymentMethod),
              const SizedBox(height: 8),
              RadioGroup<String>(
                groupValue: _selectedMethod,
                onChanged: (value) {
                  setState(() {
                    _selectedMethod = value;
                  });
                },
                child: Column(
                  children: [
                    RadioListTile<String>(
                      key: const Key('invoice-payment-method-wallet'),
                      value: 'wallet',
                      title: Text(localizations.paymentMethodWallet),
                    ),
                    RadioListTile<String>(
                      key: const Key('invoice-payment-method-mobile-money'),
                      value: 'mobile-money',
                      title: Text(localizations.paymentMethodMobileMoney),
                    ),
                    RadioListTile<String>(
                      key: const Key('invoice-payment-method-card'),
                      value: 'card',
                      title: Text(localizations.paymentMethodCard),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              FilledButton(
                key: const Key('invoice-payment-continue'),
                onPressed: _selectedMethod == null
                    ? null
                    : () {
                        setState(() {
                          _step = _PaymentFlowStep.confirmation;
                        });
                      },
                child: Text(localizations.continueToConfirmation),
              ),
            ],
            if (_step == _PaymentFlowStep.confirmation) ...[
              Card(
                key: const Key('invoice-payment-confirmation'),
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Text(
                        localizations.confirmPaymentQuestion,
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      const SizedBox(height: 16),
                      _SummaryRow(
                        label: localizations.paymentMethod,
                        value: _paymentMethodLabel(localizations),
                      ),
                      const Divider(),
                      _SummaryRow(
                        label: localizations.price,
                        value:
                            '${localizations.formatCurrency(invoice.amount)} ${invoice.currency}',
                      ),
                      const SizedBox(height: 16),
                      Text(localizations.confirmPaymentDisclaimer),
                      const SizedBox(height: 16),
                      FilledButton(
                        key: const Key('invoice-payment-confirm'),
                        onPressed: _confirmPayment,
                        child: Text(localizations.confirmInvoicePayment),
                      ),
                      const SizedBox(height: 8),
                      TextButton(
                        key: const Key('invoice-payment-change-method'),
                        onPressed: () {
                          setState(() {
                            _step = _PaymentFlowStep.method;
                          });
                        },
                        child: Text(localizations.paymentMethod),
                      ),
                    ],
                  ),
                ),
              ),
            ],
            if (_step == _PaymentFlowStep.processing) ...[
              Card(
                key: const Key('invoice-payment-processing'),
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Column(
                    children: [
                      const CircularProgressIndicator(),
                      const SizedBox(height: 16),
                      Text(
                        localizations.paymentProcessing,
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      const SizedBox(height: 8),
                      Text(localizations.confirmPaymentDisclaimer),
                    ],
                  ),
                ),
              ),
            ],
            if (_step == _PaymentFlowStep.result) ...[
              if (!widget.simulateFailure)
                Card(
                  key: const Key('invoice-payment-result-success'),
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        const Icon(
                          Icons.check_circle_outline,
                          size: 56,
                        ),
                        const SizedBox(height: 16),
                        Text(
                          localizations.paymentSuccessful,
                          textAlign: TextAlign.center,
                          style: Theme.of(context).textTheme.headlineSmall,
                        ),
                        const SizedBox(height: 8),
                        Text(
                          localizations.paymentSuccessMessage,
                          textAlign: TextAlign.center,
                        ),
                        const SizedBox(height: 16),
                        _SummaryRow(
                          label: localizations.paymentReference,
                          value: paymentReference,
                        ),
                        const Divider(),
                        _SummaryRow(
                          label: localizations.paymentMethod,
                          value: _paymentMethodLabel(localizations),
                        ),
                        const SizedBox(height: 16),
                        Text(localizations.confirmPaymentDisclaimer),
                        const SizedBox(height: 20),
                        FilledButton(
                          key: const Key('invoice-payment-view-receipt'),
                          onPressed: () {
                            setState(() {
                              _step = _PaymentFlowStep.receipt;
                            });
                          },
                          child: Text(localizations.viewReceipt),
                        ),
                        const SizedBox(height: 8),
                        TextButton(
                          key: const Key('invoice-payment-done'),
                          onPressed: () => Navigator.of(context).pop(),
                          child: Text(localizations.done),
                        ),
                        const SizedBox(height: 8),
                        OutlinedButton(
                          key: const Key('invoice-payment-back-to-invoices'),
                          onPressed: _returnToInvoices,
                          child: Text(localizations.backToInvoices),
                        ),
                      ],
                    ),
                  ),
                ),
              if (widget.simulateFailure)
                Card(
                  key: const Key('invoice-payment-result-failure'),
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        const Icon(
                          Icons.error_outline,
                          size: 56,
                        ),
                        const SizedBox(height: 16),
                        Text(
                          localizations.paymentFailed,
                          textAlign: TextAlign.center,
                          style: Theme.of(context).textTheme.headlineSmall,
                        ),
                        const SizedBox(height: 8),
                        Text(
                          localizations.paymentFailureMessage,
                          textAlign: TextAlign.center,
                        ),
                        const SizedBox(height: 16),
                        _SummaryRow(
                          label: localizations.paymentMethod,
                          value: _paymentMethodLabel(localizations),
                        ),
                        const SizedBox(height: 16),
                        Text(localizations.confirmPaymentDisclaimer),
                        const SizedBox(height: 20),
                        FilledButton(
                          key: const Key('invoice-payment-retry'),
                          onPressed: _retryPayment,
                          child: Text(localizations.retry),
                        ),
                        const SizedBox(height: 8),
                        OutlinedButton(
                          key: const Key('invoice-payment-back-to-invoices'),
                          onPressed: _returnToInvoices,
                          child: Text(localizations.backToInvoices),
                        ),
                      ],
                    ),
                  ),
                ),
            ],
            if (_step == _PaymentFlowStep.receipt) ...[
              Card(
                key: const Key('invoice-payment-receipt'),
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      const Icon(
                        Icons.receipt_long_outlined,
                        size: 56,
                      ),
                      const SizedBox(height: 16),
                      Text(
                        localizations.paymentReceipt,
                        textAlign: TextAlign.center,
                        style: Theme.of(context).textTheme.headlineSmall,
                      ),
                      const SizedBox(height: 20),
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
                        label: localizations.paymentMethod,
                        value: _paymentMethodLabel(localizations),
                      ),
                      const Divider(),
                      _SummaryRow(
                        label: localizations.paymentReference,
                        value: paymentReference,
                      ),
                      const Divider(),
                      _SummaryRow(
                        label: localizations.invoiceStatus,
                        value: localizations.paymentSuccessful,
                      ),
                      const SizedBox(height: 16),
                      Text(localizations.confirmPaymentDisclaimer),
                      const SizedBox(height: 20),
                      OutlinedButton.icon(
                        key: const Key(
                          'invoice-payment-receipt-copy-reference',
                        ),
                        onPressed: () => _copyPaymentReference(
                          paymentReference,
                          localizations,
                        ),
                        icon: const Icon(Icons.copy_outlined),
                        label: Text(localizations.copyReference),
                      ),
                      const SizedBox(height: 8),
                      OutlinedButton.icon(
                        key: const Key('invoice-payment-receipt-share'),
                        onPressed: () => _shareReceipt(
                          localizations,
                          invoice,
                          paymentReference,
                        ),
                        icon: const Icon(Icons.share_outlined),
                        label: Text(localizations.shareReceipt),
                      ),
                      const SizedBox(height: 8),
                      OutlinedButton.icon(
                        key: const Key('invoice-payment-receipt-download'),
                        onPressed: _isExportingReceipt
                            ? null
                            : () => _exportReceipt(
                                  localizations,
                                  invoice,
                                  paymentReference,
                                ),
                        icon: _isExportingReceipt
                            ? const SizedBox(
                                width: 18,
                                height: 18,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                ),
                              )
                            : const Icon(Icons.download_outlined),
                        label: Text(localizations.downloadReceipt),
                      ),
                      const SizedBox(height: 8),
                      FilledButton(
                        key: const Key('invoice-payment-receipt-done'),
                        onPressed: () => Navigator.of(context).pop(),
                        child: Text(localizations.done),
                      ),
                      const SizedBox(height: 8),
                      OutlinedButton(
                        key: const Key('invoice-payment-receipt-back-to-invoices'),
                        onPressed: _returnToInvoices,
                        child: Text(localizations.backToInvoices),
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

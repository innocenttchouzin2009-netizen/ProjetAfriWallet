import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:printing/printing.dart';
import 'package:qr_flutter/qr_flutter.dart';
import 'package:share_plus/share_plus.dart';

import '../l10n/app_localizations.dart';
import '../models/subscription_models.dart';
import '../models/subscription_receipt_verification.dart';
import '../services/subscription_invoice_receipt_pdf_service.dart';
import '../services/subscription_receipt_verification_codec.dart';
import 'subscription_receipt_verification_page.dart';

enum _PaymentFlowStep { method, confirmation, processing, result, receipt }

typedef ReceiptShareAction = Future<void> Function(String shareText);
typedef ReceiptPrintAction = Future<bool> Function(
  Uint8List pdfBytes,
  String documentName,
);

class SubscriptionInvoicePaymentEntryPage extends StatefulWidget {
  const SubscriptionInvoicePaymentEntryPage({
    super.key,
    required this.invoice,
    this.simulateFailure = false,
    this.receiptPdfService,
    this.shareReceiptAction,
    this.printReceiptAction,
  });

  final SubscriptionInvoice invoice;
  final bool simulateFailure;
  final SubscriptionInvoiceReceiptPdfService? receiptPdfService;
  final ReceiptShareAction? shareReceiptAction;
  final ReceiptPrintAction? printReceiptAction;

  @override
  State<SubscriptionInvoicePaymentEntryPage> createState() =>
      _SubscriptionInvoicePaymentEntryPageState();
}

class _SubscriptionInvoicePaymentEntryPageState
    extends State<SubscriptionInvoicePaymentEntryPage> {
  static const _receiptVerificationValidity = Duration(hours: 24);
  static const _receiptVerificationCodec = SubscriptionReceiptVerificationCodec();

  String? _selectedMethod;
  _PaymentFlowStep _step = _PaymentFlowStep.method;
  bool _isExportingReceipt = false;
  bool _isPrintingReceipt = false;
  int? _receiptVerificationIssuedAtEpochSeconds;

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

  String _receiptVerificationCode(
    SubscriptionInvoice invoice,
    String paymentReference,
  ) {
    final issuedAt = _receiptVerificationIssuedAtEpochSeconds ??
        DateTime.now().toUtc().millisecondsSinceEpoch ~/ 1000;
    final expiresAt = issuedAt + _receiptVerificationValidity.inSeconds;
    return _receiptVerificationCodec.encode(
      SubscriptionReceiptVerificationPayload(
        version: SubscriptionReceiptVerificationCodec.currentVersion,
        invoiceId: invoice.id,
        paymentReference: paymentReference,
        status: SubscriptionReceiptVerificationStatus.success,
        issuedAtEpochSeconds: issuedAt,
        expiresAtEpochSeconds: expiresAt,
      ),
    );
  }

  SubscriptionInvoiceReceiptPdfData _receiptPdfData(
    AppLocalizations localizations,
    SubscriptionInvoice invoice,
    String paymentReference,
  ) {
    return SubscriptionInvoiceReceiptPdfData(
      brandName: 'AfWal',
      title: localizations.paymentReceiptDocument,
      invoiceLabel: localizations.invoiceId,
      invoiceId: invoice.id,
      amountLabel: localizations.price,
      amount: '${localizations.formatCurrency(invoice.amount)} ${invoice.currency}',
      paymentMethodLabel: localizations.paymentMethod,
      paymentMethod: _paymentMethodLabel(localizations),
      paymentReferenceLabel: localizations.paymentReference,
      paymentReference: paymentReference,
      statusLabel: localizations.invoiceStatus,
      status: localizations.paymentSuccessful,
      verificationLabel: localizations.receiptVerificationQr,
      verificationCode: _receiptVerificationCode(invoice, paymentReference),
      disclaimer: localizations.confirmPaymentDisclaimer,
    );
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
    if (!mounted) return;
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
    final shareText = _receiptShareText(localizations, invoice, paymentReference);
    final action = widget.shareReceiptAction;
    if (action != null) {
      await action(shareText);
      return;
    }
    await SharePlus.instance.share(ShareParams(text: shareText));
  }

  Future<void> _printReceipt(
    AppLocalizations localizations,
    SubscriptionInvoice invoice,
    String paymentReference,
  ) async {
    if (_isPrintingReceipt) return;
    setState(() => _isPrintingReceipt = true);
    final service =
        widget.receiptPdfService ?? SubscriptionInvoiceReceiptPdfService();
    try {
      final pdfBytes = await service.buildPdfBytes(
        _receiptPdfData(localizations, invoice, paymentReference),
      );
      final documentName = service.fileNameForInvoice(invoice.id);
      final action = widget.printReceiptAction;
      final started = action != null
          ? await action(pdfBytes, documentName)
          : await Printing.layoutPdf(
              name: documentName,
              onLayout: (_) async => pdfBytes,
            );
      if (!started && mounted) {
        _showReceiptError(
          localizations.receiptPrintFailed,
          'invoice-payment-receipt-print-error',
        );
      }
    } catch (_) {
      if (mounted) {
        _showReceiptError(
          localizations.receiptPrintFailed,
          'invoice-payment-receipt-print-error',
        );
      }
    } finally {
      if (mounted) setState(() => _isPrintingReceipt = false);
    }
  }

  Future<void> _exportReceipt(
    AppLocalizations localizations,
    SubscriptionInvoice invoice,
    String paymentReference,
  ) async {
    if (_isExportingReceipt) return;
    setState(() => _isExportingReceipt = true);
    final service =
        widget.receiptPdfService ?? SubscriptionInvoiceReceiptPdfService();
    try {
      final result = await service.export(
        invoiceId: invoice.id,
        data: _receiptPdfData(localizations, invoice, paymentReference),
      );
      if (!mounted) return;
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
      if (mounted) {
        _showReceiptError(
          localizations.receiptGenerationFailed,
          'invoice-payment-receipt-download-error',
        );
      }
    } finally {
      if (mounted) setState(() => _isExportingReceipt = false);
    }
  }

  void _showReceiptError(String message, String key) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message, key: Key(key))));
  }

  Future<void> _confirmPayment() async {
    setState(() => _step = _PaymentFlowStep.processing);
    await Future<void>.delayed(const Duration(milliseconds: 250));
    if (!mounted) return;
    setState(() {
      if (!widget.simulateFailure) {
        _receiptVerificationIssuedAtEpochSeconds =
            DateTime.now().toUtc().millisecondsSinceEpoch ~/ 1000;
      }
      _step = _PaymentFlowStep.result;
    });
  }

  void _retryPayment() {
    setState(() {
      _selectedMethod = null;
      _receiptVerificationIssuedAtEpochSeconds = null;
      _step = _PaymentFlowStep.method;
    });
  }

  void _returnToInvoices() {
    final navigator = Navigator.of(context);
    if (navigator.canPop()) navigator.pop();
    if (navigator.canPop()) navigator.pop();
  }

  @override
  Widget build(BuildContext context) {
    final localizations = AppLocalizations.of(context)!;
    final invoice = widget.invoice;
    final paymentReference = 'BETA-${invoice.id}';
    final verificationCode = !widget.simulateFailure &&
            _receiptVerificationIssuedAtEpochSeconds != null
        ? _receiptVerificationCode(invoice, paymentReference)
        : null;

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
                onChanged: (value) => setState(() => _selectedMethod = value),
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
                    : () => setState(
                          () => _step = _PaymentFlowStep.confirmation,
                        ),
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
                        onPressed: () => setState(
                          () => _step = _PaymentFlowStep.method,
                        ),
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
                      Semantics(
                        liveRegion: true,
                        label: localizations.paymentProcessing,
                        child: ExcludeSemantics(
                          child: Text(
                            localizations.paymentProcessing,
                            style: Theme.of(context).textTheme.titleMedium,
                          ),
                        ),
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
                        const Icon(Icons.check_circle_outline, size: 56),
                        const SizedBox(height: 16),
                        Semantics(
                          liveRegion: true,
                          label: localizations.paymentSuccessful,
                          child: ExcludeSemantics(
                            child: Text(
                              localizations.paymentSuccessful,
                              textAlign: TextAlign.center,
                              style: Theme.of(context).textTheme.headlineSmall,
                            ),
                          ),
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
                          onPressed: () => setState(
                            () => _step = _PaymentFlowStep.receipt,
                          ),
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
                        const Icon(Icons.error_outline, size: 56),
                        const SizedBox(height: 16),
                        Semantics(
                          liveRegion: true,
                          label: localizations.paymentFailed,
                          child: ExcludeSemantics(
                            child: Text(
                              localizations.paymentFailed,
                              textAlign: TextAlign.center,
                              style: Theme.of(context).textTheme.headlineSmall,
                            ),
                          ),
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
            if (_step == _PaymentFlowStep.receipt &&
                verificationCode != null) ...[
              Card(
                key: const Key('invoice-payment-receipt'),
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      const Icon(Icons.receipt_long_outlined, size: 56),
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
                      Semantics(
                        label: localizations.receiptVerificationQr,
                        image: true,
                        child: Center(
                          child: QrImageView(
                            key: const Key(
                              'invoice-payment-receipt-verification-qr',
                            ),
                            data: verificationCode,
                            version: QrVersions.auto,
                            size: 180,
                          ),
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        localizations.receiptVerificationHint,
                        textAlign: TextAlign.center,
                      ),
                      const SizedBox(height: 16),
                      OutlinedButton.icon(
                        key: const Key('invoice-payment-receipt-verify'),
                        onPressed: () => Navigator.of(context).push(
                          MaterialPageRoute<void>(
                            builder: (_) => SubscriptionReceiptVerificationPage(
                              rawCode: verificationCode,
                            ),
                          ),
                        ),
                        icon: const Icon(Icons.verified_outlined),
                        label: Text(localizations.verifyReceipt),
                      ),
                      const SizedBox(height: 8),
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
                                child: CircularProgressIndicator(strokeWidth: 2),
                              )
                            : const Icon(Icons.download_outlined),
                        label: Text(localizations.downloadReceipt),
                      ),
                      const SizedBox(height: 8),
                      OutlinedButton.icon(
                        key: const Key('invoice-payment-receipt-print'),
                        onPressed: _isPrintingReceipt
                            ? null
                            : () => _printReceipt(
                                  localizations,
                                  invoice,
                                  paymentReference,
                                ),
                        icon: _isPrintingReceipt
                            ? const SizedBox(
                                width: 18,
                                height: 18,
                                child: CircularProgressIndicator(strokeWidth: 2),
                              )
                            : const Icon(Icons.print_outlined),
                        label: Text(
                          _isPrintingReceipt
                              ? localizations.receiptPrintPreparing
                              : localizations.printReceipt,
                        ),
                      ),
                      const SizedBox(height: 8),
                      FilledButton(
                        key: const Key('invoice-payment-receipt-done'),
                        onPressed: () => Navigator.of(context).pop(),
                        child: Text(localizations.done),
                      ),
                      const SizedBox(height: 8),
                      OutlinedButton(
                        key: const Key(
                          'invoice-payment-receipt-back-to-invoices',
                        ),
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
    return MergeSemantics(
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(label, style: Theme.of(context).textTheme.labelMedium),
            const SizedBox(height: 4),
            Text(value, style: Theme.of(context).textTheme.bodyLarge),
          ],
        ),
      ),
    );
  }
}

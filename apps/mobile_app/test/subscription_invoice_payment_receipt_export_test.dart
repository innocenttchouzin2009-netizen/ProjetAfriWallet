import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/pages/subscription_invoice_payment_entry_page.dart';
import 'package:mobile_app/services/subscription_invoice_receipt_pdf_service.dart';

const _invoice = SubscriptionInvoice(
  id: 'invoice-beta130-pending',
  subscriptionId: 'subscription-beta130',
  amount: 79.90,
  currency: 'EUR',
  status: 'PENDING',
  issueDate: '2026-09-06',
);

class _FakeReceiptPdfService extends SubscriptionInvoiceReceiptPdfService {
  _FakeReceiptPdfService({this.shouldFail = false});

  final bool shouldFail;
  String? exportedInvoiceId;
  SubscriptionInvoiceReceiptPdfData? exportedData;

  @override
  Future<SubscriptionInvoiceReceiptPdfExportResult> export({
    required String invoiceId,
    required SubscriptionInvoiceReceiptPdfData data,
  }) async {
    if (shouldFail) {
      throw StateError('simulated export failure');
    }

    exportedInvoiceId = invoiceId;
    exportedData = data;

    return SubscriptionInvoiceReceiptPdfExportResult(
      fileName: fileNameForInvoice(invoiceId),
      path: '/tmp/${fileNameForInvoice(invoiceId)}',
      bytesLength: 256,
    );
  }
}

Widget _app({
  Locale locale = const Locale('en'),
  bool simulateFailure = false,
  SubscriptionInvoiceReceiptPdfService? receiptPdfService,
}) =>
    MaterialApp(
      locale: locale,
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: SubscriptionInvoicePaymentEntryPage(
        invoice: _invoice,
        simulateFailure: simulateFailure,
        receiptPdfService: receiptPdfService,
      ),
    );

Future<void> _ensureVisible(WidgetTester tester, Finder finder) async {
  final listView = find.byKey(
    const Key('subscription-invoice-payment-entry-page'),
  );

  for (var i = 0; i < 16 && finder.evaluate().isEmpty; i++) {
    await tester.drag(listView, const Offset(0, -240));
    await tester.pumpAndSettle();
  }

  expect(finder, findsOneWidget);
  await tester.ensureVisible(finder);
  await tester.pumpAndSettle();
}

Future<void> _reachResult(
  WidgetTester tester, {
  Locale locale = const Locale('en'),
  bool simulateFailure = false,
  SubscriptionInvoiceReceiptPdfService? receiptPdfService,
}) async {
  await tester.pumpWidget(
    _app(
      locale: locale,
      simulateFailure: simulateFailure,
      receiptPdfService: receiptPdfService,
    ),
  );
  await tester.pumpAndSettle();

  final method = find.byKey(const Key('invoice-payment-method-wallet'));
  await _ensureVisible(tester, method);
  await tester.tap(method);
  await tester.pumpAndSettle();

  final continueButton = find.byKey(const Key('invoice-payment-continue'));
  await _ensureVisible(tester, continueButton);
  await tester.tap(continueButton);
  await tester.pumpAndSettle();

  final confirmButton = find.byKey(const Key('invoice-payment-confirm'));
  await _ensureVisible(tester, confirmButton);
  await tester.tap(confirmButton);
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 300));
  await tester.pumpAndSettle();
}

Future<void> _openReceipt(
  WidgetTester tester, {
  Locale locale = const Locale('en'),
  required SubscriptionInvoiceReceiptPdfService receiptPdfService,
}) async {
  await _reachResult(
    tester,
    locale: locale,
    receiptPdfService: receiptPdfService,
  );

  final viewReceipt = find.byKey(const Key('invoice-payment-view-receipt'));
  await _ensureVisible(tester, viewReceipt);
  await tester.tap(viewReceipt);
  await tester.pumpAndSettle();

  expect(find.byKey(const Key('invoice-payment-receipt')), findsOneWidget);
}

void main() {
  test('receipt PDF service creates deterministic file name', () {
    final service = SubscriptionInvoiceReceiptPdfService();

    expect(
      service.fileNameForInvoice('invoice beta130/pending'),
      'afwal-receipt-invoice-beta130-pending.pdf',
    );
    expect(
      service.fileNameForInvoice(_invoice.id),
      'afwal-receipt-invoice-beta130-pending.pdf',
    );
  });

  testWidgets('successful receipt exports expected PDF data and shows feedback',
      (tester) async {
    final service = _FakeReceiptPdfService();

    await _openReceipt(tester, receiptPdfService: service);

    final download = find.byKey(
      const Key('invoice-payment-receipt-download'),
    );
    await _ensureVisible(tester, download);
    expect(find.text('Download receipt'), findsOneWidget);

    await tester.tap(download);
    await tester.pump();
    await tester.pumpAndSettle();

    expect(service.exportedInvoiceId, _invoice.id);
    expect(service.exportedData, isNotNull);
    expect(service.exportedData!.brandName, 'AfWal');
    expect(service.exportedData!.invoiceId, 'invoice-beta130-pending');
    expect(service.exportedData!.amount, '79.90 EUR');
    expect(service.exportedData!.paymentMethod, 'AfWal balance');
    expect(
      service.exportedData!.paymentReference,
      'BETA-invoice-beta130-pending',
    );
    expect(service.exportedData!.status, 'Payment successful');
    expect(
      service.exportedData!.disclaimer,
      'No money will be moved in this beta flow.',
    );
    expect(
      find.byKey(const Key('invoice-payment-receipt-download-feedback')),
      findsOneWidget,
    );
    expect(
      find.textContaining('afwal-receipt-invoice-beta130-pending.pdf'),
      findsOneWidget,
    );
  });

  testWidgets('download action is not exposed for failed payment result',
      (tester) async {
    await _reachResult(tester, simulateFailure: true);

    expect(
      find.byKey(const Key('invoice-payment-result-failure')),
      findsOneWidget,
    );
    expect(
      find.byKey(const Key('invoice-payment-receipt-download')),
      findsNothing,
    );
    expect(
      find.byKey(const Key('invoice-payment-receipt-download-feedback')),
      findsNothing,
    );
  });

  testWidgets('French receipt export action and PDF data are localized',
      (tester) async {
    final service = _FakeReceiptPdfService();

    await _openReceipt(
      tester,
      locale: const Locale('fr'),
      receiptPdfService: service,
    );

    final download = find.byKey(
      const Key('invoice-payment-receipt-download'),
    );
    await _ensureVisible(tester, download);
    expect(find.text('Télécharger le reçu'), findsOneWidget);

    await tester.tap(download);
    await tester.pump();
    await tester.pumpAndSettle();

    expect(service.exportedData, isNotNull);
    expect(service.exportedData!.title, 'Document de reçu de paiement');
    expect(service.exportedData!.paymentMethod, 'Solde AfWal');
    expect(service.exportedData!.status, 'Paiement réussi');
    expect(
      find.byKey(const Key('invoice-payment-receipt-download-feedback')),
      findsOneWidget,
    );
    expect(find.textContaining('Reçu généré'), findsOneWidget);
  });

  testWidgets('receipt export failure shows controlled error feedback',
      (tester) async {
    final service = _FakeReceiptPdfService(shouldFail: true);

    await _openReceipt(tester, receiptPdfService: service);

    final download = find.byKey(
      const Key('invoice-payment-receipt-download'),
    );
    await _ensureVisible(tester, download);
    await tester.tap(download);
    await tester.pump();
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('invoice-payment-receipt-download-error')),
      findsOneWidget,
    );
    expect(find.text('Receipt generation failed'), findsOneWidget);
  });
}

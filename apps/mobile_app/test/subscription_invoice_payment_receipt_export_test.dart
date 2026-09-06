import 'dart:io';

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

SubscriptionInvoiceReceiptPdfData _pdfData() =>
    const SubscriptionInvoiceReceiptPdfData(
      brandName: 'AfWal',
      title: 'Payment receipt document',
      invoiceLabel: 'Invoice',
      invoiceId: 'invoice-beta130-pending',
      amountLabel: 'Price',
      amount: '79.90 EUR',
      paymentMethodLabel: 'Payment method',
      paymentMethod: 'AfWal balance',
      paymentReferenceLabel: 'Payment reference',
      paymentReference: 'BETA-invoice-beta130-pending',
      statusLabel: 'Status',
      status: 'Payment successful',
      disclaimer: 'No money will be moved in this beta flow.',
    );

void main() {
  test('receipt PDF service creates deterministic local PDF export', () async {
    final tempDirectory = await Directory.systemTemp.createTemp('afwal-beta130-');
    addTearDown(() => tempDirectory.delete(recursive: true));

    final service = SubscriptionInvoiceReceiptPdfService(
      directoryProvider: () async => tempDirectory,
    );

    expect(
      service.fileNameForInvoice('invoice beta130/pending'),
      'afwal-receipt-invoice-beta130-pending.pdf',
    );

    final bytes = await service.buildPdfBytes(_pdfData());
    expect(bytes.length, greaterThan(100));
    expect(String.fromCharCodes(bytes.take(4)), '%PDF');

    final result = await service.export(
      invoiceId: _invoice.id,
      data: _pdfData(),
    );

    expect(result.fileName, 'afwal-receipt-invoice-beta130-pending.pdf');
    expect(result.bytesLength, greaterThan(100));
    expect(await File(result.path).exists(), isTrue);
    expect(await File(result.path).length(), result.bytesLength);
  });

  testWidgets('successful receipt exports PDF and shows generated feedback',
      (tester) async {
    final tempDirectory = await Directory.systemTemp.createTemp('afwal-beta130-');
    addTearDown(() => tempDirectory.delete(recursive: true));
    final service = SubscriptionInvoiceReceiptPdfService(
      directoryProvider: () async => tempDirectory,
    );

    await _openReceipt(tester, receiptPdfService: service);

    final download = find.byKey(
      const Key('invoice-payment-receipt-download'),
    );
    await _ensureVisible(tester, download);
    expect(find.text('Download receipt'), findsOneWidget);

    await tester.tap(download);
    await tester.pump();
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('invoice-payment-receipt-download-feedback')),
      findsOneWidget,
    );
    expect(
      find.textContaining('afwal-receipt-invoice-beta130-pending.pdf'),
      findsOneWidget,
    );
    expect(
      await File(
        '${tempDirectory.path}${Platform.pathSeparator}'
        'afwal-receipt-invoice-beta130-pending.pdf',
      ).exists(),
      isTrue,
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

  testWidgets('French receipt export action and feedback are localized',
      (tester) async {
    final tempDirectory = await Directory.systemTemp.createTemp('afwal-beta130-');
    addTearDown(() => tempDirectory.delete(recursive: true));
    final service = SubscriptionInvoiceReceiptPdfService(
      directoryProvider: () async => tempDirectory,
    );

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

    expect(
      find.byKey(const Key('invoice-payment-receipt-download-feedback')),
      findsOneWidget,
    );
    expect(find.textContaining('Reçu généré'), findsOneWidget);
  });

  testWidgets('receipt export failure shows controlled error feedback',
      (tester) async {
    final service = SubscriptionInvoiceReceiptPdfService(
      directoryProvider: () async => throw FileSystemException('simulated'),
    );

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

import 'dart:async';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/models/subscription_receipt_verification.dart';
import 'package:mobile_app/pages/subscription_invoice_payment_entry_page.dart';
import 'package:mobile_app/pages/subscription_receipt_verification_page.dart';
import 'package:mobile_app/services/subscription_invoice_receipt_pdf_service.dart';
import 'package:mobile_app/services/subscription_receipt_verification_codec.dart';

const _invoice = SubscriptionInvoice(
  id: 'invoice-beta134-pending',
  subscriptionId: 'subscription-beta134',
  amount: 42.50,
  currency: 'EUR',
  status: 'PENDING',
  issueDate: '2026-09-07',
);

const _verificationCodec = SubscriptionReceiptVerificationCodec();

Widget _app({
  bool simulateFailure = false,
  SubscriptionInvoiceReceiptPdfService? receiptPdfService,
  ReceiptPrintAction? printReceiptAction,
}) =>
    MaterialApp(
      locale: const Locale('en'),
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: SubscriptionInvoicePaymentEntryPage(
        invoice: _invoice,
        simulateFailure: simulateFailure,
        receiptPdfService: receiptPdfService,
        printReceiptAction: printReceiptAction,
      ),
    );

Widget _verificationApp({required String rawCode, required DateTime now}) =>
    MaterialApp(
      locale: const Locale('en'),
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: SubscriptionReceiptVerificationPage(
        rawCode: rawCode,
        nowProvider: () => now,
      ),
    );

String _verificationCode({required int issuedAt, required int expiresAt}) =>
    _verificationCodec.encode(
      SubscriptionReceiptVerificationPayload(
        version: SubscriptionReceiptVerificationCodec.currentVersion,
        invoiceId: _invoice.id,
        paymentReference: 'BETA-${_invoice.id}',
        status: SubscriptionReceiptVerificationStatus.success,
        issuedAtEpochSeconds: issuedAt,
        expiresAtEpochSeconds: expiresAt,
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

Future<void> _startPayment(WidgetTester tester) async {
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
}

Future<void> _openReceipt(WidgetTester tester) async {
  await _startPayment(tester);
  await tester.pump(const Duration(milliseconds: 300));
  await tester.pumpAndSettle();

  final viewReceipt = find.byKey(const Key('invoice-payment-view-receipt'));
  await _ensureVisible(tester, viewReceipt);
  await tester.tap(viewReceipt);
  await tester.pumpAndSettle();
}

void _expectLiveRegionForText(WidgetTester tester, String text) {
  final textFinder = find.text(text);
  expect(textFinder, findsOneWidget);

  final semanticsFinder = find.ancestor(
    of: textFinder,
    matching: find.byType(Semantics),
  );
  expect(semanticsFinder, findsWidgets);

  final semanticsWidgets = tester.widgetList<Semantics>(semanticsFinder);
  expect(
    semanticsWidgets.any(
      (semantics) =>
          semantics.properties.label == text && semantics.properties.liveRegion,
    ),
    isTrue,
  );
}

void _expectMergedSemanticsForText(WidgetTester tester, String text) {
  final textFinder = find.text(text);
  expect(textFinder, findsOneWidget);
  expect(
    find.ancestor(of: textFinder, matching: find.byType(MergeSemantics)),
    findsOneWidget,
  );
}

class _ControlledReceiptPdfService extends SubscriptionInvoiceReceiptPdfService {
  _ControlledReceiptPdfService({this.exportGate});

  final Completer<void>? exportGate;

  @override
  Future<Uint8List> buildPdfBytes(SubscriptionInvoiceReceiptPdfData data) async {
    return Uint8List.fromList(const [1, 2, 3]);
  }

  @override
  Future<SubscriptionInvoiceReceiptPdfExportResult> export({
    required String invoiceId,
    required SubscriptionInvoiceReceiptPdfData data,
  }) async {
    final gate = exportGate;
    if (gate != null) await gate.future;
    return const SubscriptionInvoiceReceiptPdfExportResult(
      fileName: 'receipt.pdf',
      path: '/tmp/receipt.pdf',
      bytesLength: 3,
    );
  }
}

void main() {
  testWidgets('summary rows expose merged label and value semantics',
      (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    _expectMergedSemanticsForText(tester, 'Invoice ID');
    _expectMergedSemanticsForText(tester, 'invoice-beta134-pending');
    _expectMergedSemanticsForText(tester, 'Price');
    _expectMergedSemanticsForText(tester, '42.50 EUR');
  });

  testWidgets('processing and success are announced as live regions',
      (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();
    await _startPayment(tester);

    _expectLiveRegionForText(tester, 'Processing payment');

    await tester.pump(const Duration(milliseconds: 300));
    await tester.pumpAndSettle();

    final success = find.byKey(const Key('invoice-payment-result-success'));
    await _ensureVisible(tester, success);
    _expectLiveRegionForText(tester, 'Payment successful');
  });

  testWidgets('failure is announced as a live region', (tester) async {
    await tester.pumpWidget(_app(simulateFailure: true));
    await tester.pumpAndSettle();
    await _startPayment(tester);

    _expectLiveRegionForText(tester, 'Processing payment');

    await tester.pump(const Duration(milliseconds: 300));
    await tester.pumpAndSettle();

    final failure = find.byKey(const Key('invoice-payment-result-failure'));
    await _ensureVisible(tester, failure);
    _expectLiveRegionForText(tester, 'Payment failed');
  });

  testWidgets('valid verification state is announced and rows are merged',
      (tester) async {
    final now = DateTime.fromMillisecondsSinceEpoch(2000 * 1000, isUtc: true);
    await tester.pumpWidget(
      _verificationApp(
        rawCode: _verificationCode(issuedAt: 1000, expiresAt: 3000),
        now: now,
      ),
    );
    await tester.pumpAndSettle();

    _expectLiveRegionForText(tester, 'Valid receipt');
    _expectMergedSemanticsForText(tester, 'Invoice ID');
    _expectMergedSemanticsForText(tester, 'invoice-beta134-pending');
    _expectMergedSemanticsForText(tester, 'Payment reference');
    _expectMergedSemanticsForText(
      tester,
      'BETA-invoice-beta134-pending',
    );
  });

  testWidgets('invalid verification state is announced as a live region',
      (tester) async {
    await tester.pumpWidget(
      _verificationApp(
        rawCode: 'AFW|static|merchant|10.00|EUR',
        now: DateTime.utc(2026, 9, 7),
      ),
    );
    await tester.pumpAndSettle();

    _expectLiveRegionForText(tester, 'Invalid receipt');
  });

  testWidgets('expired verification state is announced as a live region',
      (tester) async {
    final now = DateTime.fromMillisecondsSinceEpoch(4000 * 1000, isUtc: true);
    await tester.pumpWidget(
      _verificationApp(
        rawCode: _verificationCode(issuedAt: 1000, expiresAt: 3000),
        now: now,
      ),
    );
    await tester.pumpAndSettle();

    _expectLiveRegionForText(tester, 'Expired receipt');
  });

  testWidgets('receipt is announced and core actions expose accessible labels',
      (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();
    await _openReceipt(tester);

    _expectLiveRegionForText(tester, 'Payment receipt');

    for (final label in [
      'Verify receipt',
      'Copy reference',
      'Share receipt',
      'Download receipt',
      'Print receipt',
      'Done',
      'Back to invoices',
    ]) {
      final action = find.text(label);
      await _ensureVisible(tester, action);
      expect(action, findsOneWidget);
    }
  });

  testWidgets('download and print announce their preparing states',
      (tester) async {
    final exportGate = Completer<void>();
    final printGate = Completer<void>();
    final service = _ControlledReceiptPdfService(exportGate: exportGate);

    await tester.pumpWidget(
      _app(
        receiptPdfService: service,
        printReceiptAction: (_, _) async {
          await printGate.future;
          return true;
        },
      ),
    );
    await tester.pumpAndSettle();
    await _openReceipt(tester);

    final download = find.byKey(const Key('invoice-payment-receipt-download'));
    await _ensureVisible(tester, download);
    await tester.tap(download);
    await tester.pump();

    _expectLiveRegionForText(tester, 'Preparing receipt');
    expect(tester.widget<OutlinedButton>(download).onPressed, isNull);

    exportGate.complete();
    await tester.pumpAndSettle();

    final print = find.byKey(const Key('invoice-payment-receipt-print'));
    await _ensureVisible(tester, print);
    await tester.tap(print);
    await tester.pump();

    _expectLiveRegionForText(tester, 'Preparing receipt');
    expect(tester.widget<OutlinedButton>(print).onPressed, isNull);

    printGate.complete();
    await tester.pumpAndSettle();
  });
}

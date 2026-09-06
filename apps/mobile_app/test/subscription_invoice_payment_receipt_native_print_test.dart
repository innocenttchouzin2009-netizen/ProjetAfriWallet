import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/pages/subscription_invoice_payment_entry_page.dart';

const _invoice = SubscriptionInvoice(
  id: 'invoice-beta132-pending',
  subscriptionId: 'subscription-beta132',
  amount: 91.25,
  currency: 'EUR',
  status: 'PENDING',
  issueDate: '2026-09-06',
);

Widget _app({required ReceiptPrintAction printReceiptAction}) => MaterialApp(
      locale: const Locale('en'),
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: SubscriptionInvoicePaymentEntryPage(
        invoice: _invoice,
        printReceiptAction: printReceiptAction,
      ),
    );

Future<void> _ensureVisible(WidgetTester tester, Finder finder) async {
  final listView = find.byKey(
    const Key('subscription-invoice-payment-entry-page'),
  );

  for (var index = 0; index < 16 && finder.evaluate().isEmpty; index++) {
    await tester.drag(listView, const Offset(0, -240));
    await tester.pumpAndSettle();
  }

  expect(finder, findsOneWidget);
  await tester.ensureVisible(finder);
  await tester.pumpAndSettle();
}

Future<void> _openReceipt(
  WidgetTester tester, {
  required ReceiptPrintAction printReceiptAction,
}) async {
  await tester.pumpWidget(_app(printReceiptAction: printReceiptAction));
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
  await tester.pump(const Duration(milliseconds: 300));
  await tester.pumpAndSettle();

  final viewReceipt = find.byKey(const Key('invoice-payment-view-receipt'));
  await _ensureVisible(tester, viewReceipt);
  await tester.tap(viewReceipt);
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('receipt prepares PDF bytes and invokes native print action',
      (tester) async {
    Uint8List? printedBytes;
    String? printedName;

    await _openReceipt(
      tester,
      printReceiptAction: (bytes, name) async {
        printedBytes = bytes;
        printedName = name;
        return true;
      },
    );

    final printButton = find.byKey(const Key('invoice-payment-receipt-print'));
    await _ensureVisible(tester, printButton);
    expect(find.text('Print receipt'), findsOneWidget);

    await tester.tap(printButton);
    await tester.pumpAndSettle();

    expect(printedBytes, isNotNull);
    expect(printedBytes, isNotEmpty);
    expect(printedBytes!.take(4), orderedEquals(<int>[0x25, 0x50, 0x44, 0x46]));
    expect(printedName, 'afwal-receipt-invoice-beta132-pending.pdf');
    expect(
      find.byKey(const Key('invoice-payment-receipt-print-error')),
      findsNothing,
    );
  });

  testWidgets('receipt exposes controlled feedback when native print fails',
      (tester) async {
    await _openReceipt(
      tester,
      printReceiptAction: (bytes, name) async => false,
    );

    final printButton = find.byKey(const Key('invoice-payment-receipt-print'));
    await _ensureVisible(tester, printButton);
    await tester.tap(printButton);
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('invoice-payment-receipt-print-error')),
      findsOneWidget,
    );
    expect(find.text('Receipt printing could not be started'), findsOneWidget);
  });
}

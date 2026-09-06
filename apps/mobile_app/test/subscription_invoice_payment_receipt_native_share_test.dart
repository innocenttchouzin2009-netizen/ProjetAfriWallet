import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/pages/subscription_invoice_payment_entry_page.dart';

const _invoice = SubscriptionInvoice(
  id: 'invoice-beta131-pending',
  subscriptionId: 'subscription-beta131',
  amount: 91.25,
  currency: 'EUR',
  status: 'PENDING',
  issueDate: '2026-09-06',
);

Widget _app({required ReceiptShareAction shareReceiptAction}) => MaterialApp(
      locale: const Locale('en'),
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: SubscriptionInvoicePaymentEntryPage(
        invoice: _invoice,
        shareReceiptAction: shareReceiptAction,
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
  required ReceiptShareAction shareReceiptAction,
}) async {
  await tester.pumpWidget(_app(shareReceiptAction: shareReceiptAction));
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
  testWidgets('receipt shares localized payment data through the native action',
      (tester) async {
    String? sharedText;
    await _openReceipt(
      tester,
      shareReceiptAction: (text) async {
        sharedText = text;
      },
    );

    final share = find.byKey(const Key('invoice-payment-receipt-share'));
    await _ensureVisible(tester, share);
    await tester.tap(share);
    await tester.pump();

    expect(sharedText, contains('Payment receipt'));
    expect(sharedText, contains('invoice-beta131-pending'));
    expect(sharedText, contains('91.25 EUR'));
    expect(sharedText, contains('AfWal balance'));
    expect(sharedText, contains('BETA-invoice-beta131-pending'));
    expect(sharedText, contains('Payment successful'));
    expect(
      find.byKey(const Key('invoice-payment-receipt-share-content')),
      findsNothing,
    );
  });
}
import 'package:flutter/material.dart';
import 'package:flutter/semantics.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/pages/subscription_invoice_payment_entry_page.dart';

const _invoice = SubscriptionInvoice(
  id: 'invoice-beta134-pending',
  subscriptionId: 'subscription-beta134',
  amount: 42.50,
  currency: 'EUR',
  status: 'PENDING',
  issueDate: '2026-09-07',
);

Widget _app({bool simulateFailure = false}) => MaterialApp(
      locale: const Locale('en'),
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: SubscriptionInvoicePaymentEntryPage(
        invoice: _invoice,
        simulateFailure: simulateFailure,
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

void _expectLiveRegion(WidgetTester tester, String label) {
  final semantics = find.bySemanticsLabel(label);
  expect(semantics, findsOneWidget);
  final node = tester.getSemantics(semantics);
  expect(node.hasFlag(SemanticsFlag.isLiveRegion), isTrue);
}

void main() {
  testWidgets('summary rows expose merged label and value semantics',
      (tester) async {
    final semantics = tester.ensureSemantics();
    addTearDown(semantics.dispose);

    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    expect(
      find.bySemanticsLabel(
        RegExp(r'Invoice ID.*invoice-beta134-pending', dotAll: true),
      ),
      findsOneWidget,
    );
    expect(
      find.bySemanticsLabel(RegExp(r'Price.*42\.50 EUR', dotAll: true)),
      findsOneWidget,
    );
  });

  testWidgets('processing and success are announced as live regions',
      (tester) async {
    final semantics = tester.ensureSemantics();
    addTearDown(semantics.dispose);

    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();
    await _startPayment(tester);

    _expectLiveRegion(tester, 'Processing payment');

    await tester.pump(const Duration(milliseconds: 300));
    await tester.pumpAndSettle();

    final success = find.byKey(const Key('invoice-payment-result-success'));
    await _ensureVisible(tester, success);
    _expectLiveRegion(tester, 'Payment successful');
  });

  testWidgets('failure is announced as a live region', (tester) async {
    final semantics = tester.ensureSemantics();
    addTearDown(semantics.dispose);

    await tester.pumpWidget(_app(simulateFailure: true));
    await tester.pumpAndSettle();
    await _startPayment(tester);

    _expectLiveRegion(tester, 'Processing payment');

    await tester.pump(const Duration(milliseconds: 300));
    await tester.pumpAndSettle();

    final failure = find.byKey(const Key('invoice-payment-result-failure'));
    await _ensureVisible(tester, failure);
    _expectLiveRegion(tester, 'Payment failed');
  });
}

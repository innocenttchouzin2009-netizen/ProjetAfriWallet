import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/pages/subscription_invoice_payment_entry_page.dart';

const _invoice = SubscriptionInvoice(
  id: 'invoice-beta128-pending',
  subscriptionId: 'subscription-beta128',
  amount: 49.99,
  currency: 'EUR',
  status: 'PENDING',
  issueDate: '2026-09-06',
);

Widget _app({
  Locale locale = const Locale('en'),
  bool simulateFailure = false,
}) =>
    MaterialApp(
      locale: locale,
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

  for (var i = 0; i < 12 && finder.evaluate().isEmpty; i++) {
    await tester.drag(listView, const Offset(0, -240));
    await tester.pumpAndSettle();
  }

  expect(finder, findsOneWidget);
  await tester.ensureVisible(finder);
  await tester.pumpAndSettle();
}

Future<void> _reachSuccess(WidgetTester tester) async {
  await tester.pumpWidget(_app());
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

  expect(
    find.byKey(const Key('invoice-payment-result-success')),
    findsOneWidget,
  );
}

Future<void> _openReceipt(WidgetTester tester) async {
  final viewReceipt = find.byKey(const Key('invoice-payment-view-receipt'));
  await _ensureVisible(tester, viewReceipt);
  await tester.tap(viewReceipt);
  await tester.pumpAndSettle();

  final receipt = find.byKey(const Key('invoice-payment-receipt'));
  await _ensureVisible(tester, receipt);
  expect(receipt, findsOneWidget);
}

void main() {
  testWidgets('successful payment exposes receipt completion action',
      (tester) async {
    await _reachSuccess(tester);

    final viewReceipt = find.byKey(const Key('invoice-payment-view-receipt'));
    await _ensureVisible(tester, viewReceipt);
    expect(viewReceipt, findsOneWidget);
    expect(find.text('View receipt'), findsOneWidget);
  });

  testWidgets('receipt preserves invoice payment summary and reference',
      (tester) async {
    await _reachSuccess(tester);
    await _openReceipt(tester);

    expect(find.text('Payment receipt'), findsWidgets);
    expect(find.text('invoice-beta128-pending'), findsWidgets);
    expect(find.textContaining('49.99'), findsWidgets);
    expect(find.textContaining('EUR'), findsWidgets);
    expect(find.text('AfWal balance'), findsOneWidget);
    expect(find.text('BETA-invoice-beta128-pending'), findsOneWidget);
    expect(find.text('Payment successful'), findsOneWidget);
    expect(
      find.byKey(const Key('subscription-invoice-payment-summary')),
      findsOneWidget,
    );
  });

  testWidgets('receipt exposes explicit completion and invoice return actions',
      (tester) async {
    await _reachSuccess(tester);
    await _openReceipt(tester);

    expect(
      find.byKey(const Key('invoice-payment-receipt-done')),
      findsOneWidget,
    );
    expect(
      find.byKey(const Key('invoice-payment-receipt-back-to-invoices')),
      findsOneWidget,
    );
  });

  testWidgets('failure result never exposes a receipt', (tester) async {
    await tester.pumpWidget(_app(simulateFailure: true));
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

    expect(
      find.byKey(const Key('invoice-payment-result-failure')),
      findsOneWidget,
    );
    expect(
      find.byKey(const Key('invoice-payment-view-receipt')),
      findsNothing,
    );
    expect(find.byKey(const Key('invoice-payment-receipt')), findsNothing);
  });

  testWidgets('French receipt localization is available', (tester) async {
    await tester.pumpWidget(_app(locale: const Locale('fr')));
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

    final viewReceipt = find.byKey(const Key('invoice-payment-view-receipt'));
    await _ensureVisible(tester, viewReceipt);
    expect(find.text('Voir le reçu'), findsOneWidget);

    await tester.tap(viewReceipt);
    await tester.pumpAndSettle();

    final receipt = find.byKey(const Key('invoice-payment-receipt'));
    await _ensureVisible(tester, receipt);
    expect(find.text('Reçu de paiement'), findsWidgets);
    expect(find.text('Paiement réussi'), findsOneWidget);
  });
}

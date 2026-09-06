import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/pages/subscription_invoice_detail_page.dart';
import 'package:mobile_app/pages/subscription_invoice_payment_entry_page.dart';

const _pendingInvoice = SubscriptionInvoice(
  id: 'invoice-beta125-pending',
  subscriptionId: 'subscription-beta125',
  amount: 49.99,
  currency: 'EUR',
  status: 'PENDING',
  issueDate: '2026-09-04',
);

const _paidInvoice = SubscriptionInvoice(
  id: 'invoice-beta125-paid',
  subscriptionId: 'subscription-beta125',
  amount: 49.99,
  currency: 'EUR',
  status: 'PAID',
  issueDate: '2026-09-04',
);

const _cancelledInvoice = SubscriptionInvoice(
  id: 'invoice-beta125-cancelled',
  subscriptionId: 'subscription-beta125',
  amount: 49.99,
  currency: 'EUR',
  status: 'CANCELLED',
  issueDate: '2026-09-04',
);

Widget _detailApp(SubscriptionInvoice invoice) => MaterialApp(
      locale: const Locale('en'),
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: SubscriptionInvoiceDetailPage(invoice: invoice),
    );

Widget _paymentApp({
  Locale locale = const Locale('en'),
  bool simulateFailure = false,
}) =>
    MaterialApp(
      locale: locale,
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: SubscriptionInvoicePaymentEntryPage(
        invoice: _pendingInvoice,
        simulateFailure: simulateFailure,
      ),
    );

Future<void> _ensurePaymentEntryVisible(
  WidgetTester tester,
  Finder finder,
) async {
  if (finder.evaluate().isNotEmpty) {
    await tester.ensureVisible(finder);
    await tester.pumpAndSettle();
    return;
  }

  final listView = find.byKey(
    const Key('subscription-invoice-payment-entry-page'),
  );
  expect(listView, findsOneWidget);

  for (var i = 0; i < 12 && finder.evaluate().isEmpty; i++) {
    await tester.drag(listView, const Offset(0, -240));
    await tester.pumpAndSettle();
  }

  for (var i = 0; i < 12 && finder.evaluate().isEmpty; i++) {
    await tester.drag(listView, const Offset(0, 240));
    await tester.pumpAndSettle();
  }

  expect(finder, findsOneWidget);
  await tester.ensureVisible(finder);
  await tester.pumpAndSettle();
}

Future<void> _selectMethodAndContinue(
  WidgetTester tester,
  Key methodKey,
) async {
  final methodFinder = find.byKey(methodKey);
  await _ensurePaymentEntryVisible(tester, methodFinder);
  await tester.tap(methodFinder);
  await tester.pumpAndSettle();

  final continueFinder = find.byKey(const Key('invoice-payment-continue'));
  await _ensurePaymentEntryVisible(tester, continueFinder);
  final continueButton = tester.widget<FilledButton>(continueFinder);
  expect(continueButton.onPressed, isNotNull);

  await tester.tap(continueFinder);
  await tester.pumpAndSettle();
}

Future<void> _confirmPayment(WidgetTester tester) async {
  final confirmButton = find.byKey(const Key('invoice-payment-confirm'));
  await _ensurePaymentEntryVisible(tester, confirmButton);
  await tester.tap(confirmButton);
  await tester.pump();
}

void main() {
  testWidgets('pay now is available only for payable invoices', (tester) async {
    for (final status in ['PENDING', 'OVERDUE', 'FAILED']) {
      final invoice = SubscriptionInvoice(
        id: 'invoice-$status',
        subscriptionId: 'subscription-beta125',
        amount: 49.99,
        currency: 'EUR',
        status: status,
        issueDate: '2026-09-04',
      );

      await tester.pumpWidget(_detailApp(invoice));
      await tester.pumpAndSettle();

      expect(
        find.byKey(const Key('subscription-invoice-pay-now')),
        findsOneWidget,
      );
      expect(
        find.byKey(const Key('subscription-invoice-payment-unavailable')),
        findsNothing,
      );
    }
  });

  testWidgets('paid and non-payable invoices never expose pay now', (tester) async {
    await tester.pumpWidget(_detailApp(_paidInvoice));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('subscription-invoice-pay-now')), findsNothing);
    expect(
      find.byKey(const Key('subscription-invoice-payment-unavailable')),
      findsOneWidget,
    );
    expect(find.text('This invoice has already been paid.'), findsOneWidget);

    await tester.pumpWidget(_detailApp(_cancelledInvoice));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('subscription-invoice-pay-now')), findsNothing);
    expect(
      find.byKey(const Key('subscription-invoice-payment-unavailable')),
      findsOneWidget,
    );
  });

  testWidgets('pay now opens local invoice payment entry', (tester) async {
    await tester.pumpWidget(_detailApp(_pendingInvoice));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('subscription-invoice-pay-now')));
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('subscription-invoice-payment-entry-page')),
      findsOneWidget,
    );
    expect(
      find.byKey(const Key('subscription-invoice-payment-summary')),
      findsOneWidget,
    );
    expect(find.textContaining('invoice-beta125-pending'), findsOneWidget);
    expect(find.textContaining('49.99'), findsOneWidget);
    expect(find.textContaining('EUR'), findsOneWidget);
  });

  testWidgets('continue stays disabled until a payment method is selected',
      (tester) async {
    await tester.pumpWidget(_paymentApp());
    await tester.pumpAndSettle();

    final continueFinder = find.byKey(const Key('invoice-payment-continue'));
    await _ensurePaymentEntryVisible(tester, continueFinder);

    final continueButton = tester.widget<FilledButton>(continueFinder);
    expect(continueButton.onPressed, isNull);
    expect(
      find.byKey(const Key('invoice-payment-confirmation')),
      findsNothing,
    );
  });

  testWidgets('wallet selection opens explicit confirmation step', (tester) async {
    await tester.pumpWidget(_paymentApp());
    await tester.pumpAndSettle();

    await _selectMethodAndContinue(
      tester,
      const Key('invoice-payment-method-wallet'),
    );

    final confirmation = find.byKey(const Key('invoice-payment-confirmation'));
    await _ensurePaymentEntryVisible(tester, confirmation);
    expect(confirmation, findsOneWidget);
    expect(find.text('AfWal balance'), findsOneWidget);

    final confirmButton = find.byKey(const Key('invoice-payment-confirm'));
    await _ensurePaymentEntryVisible(tester, confirmButton);
    expect(confirmButton, findsOneWidget);
  });

  testWidgets('mobile money reaches confirmation locally', (tester) async {
    await tester.pumpWidget(_paymentApp());
    await tester.pumpAndSettle();

    await _selectMethodAndContinue(
      tester,
      const Key('invoice-payment-method-mobile-money'),
    );

    final confirmation = find.byKey(const Key('invoice-payment-confirmation'));
    await _ensurePaymentEntryVisible(tester, confirmation);
    expect(confirmation, findsOneWidget);
    expect(find.text('Mobile Money'), findsOneWidget);
  });

  testWidgets('card reaches confirmation locally', (tester) async {
    await tester.pumpWidget(_paymentApp());
    await tester.pumpAndSettle();

    await _selectMethodAndContinue(
      tester,
      const Key('invoice-payment-method-card'),
    );

    final confirmation = find.byKey(const Key('invoice-payment-confirmation'));
    await _ensurePaymentEntryVisible(tester, confirmation);
    expect(confirmation, findsOneWidget);
    expect(find.text('Card'), findsOneWidget);
  });

  testWidgets('change payment method returns to selection step', (tester) async {
    await tester.pumpWidget(_paymentApp());
    await tester.pumpAndSettle();

    await _selectMethodAndContinue(
      tester,
      const Key('invoice-payment-method-wallet'),
    );

    final changeMethod = find.byKey(const Key('invoice-payment-change-method'));
    await _ensurePaymentEntryVisible(tester, changeMethod);
    await tester.tap(changeMethod);
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('invoice-payment-confirmation')), findsNothing);
    expect(
      find.byKey(const Key('invoice-payment-method-wallet')),
      findsOneWidget,
    );
    expect(find.byKey(const Key('invoice-payment-continue')), findsOneWidget);
  });

  testWidgets('confirmation transitions through processing to success result',
      (tester) async {
    await tester.pumpWidget(_paymentApp());
    await tester.pumpAndSettle();

    await _selectMethodAndContinue(
      tester,
      const Key('invoice-payment-method-wallet'),
    );

    await _confirmPayment(tester);

    expect(
      find.byKey(const Key('invoice-payment-processing')),
      findsOneWidget,
    );
    expect(
      find.byKey(const Key('invoice-payment-result-success')),
      findsNothing,
    );

    await tester.pump(const Duration(milliseconds: 300));
    await tester.pumpAndSettle();

    final result = find.byKey(const Key('invoice-payment-result-success'));
    await _ensurePaymentEntryVisible(tester, result);
    expect(result, findsOneWidget);
    expect(find.text('Payment successful'), findsOneWidget);
    expect(find.text('BETA-invoice-beta125-pending'), findsOneWidget);
    expect(find.text('AfWal balance'), findsOneWidget);
  });

  testWidgets('controlled failure reaches localized failure result', (tester) async {
    await tester.pumpWidget(_paymentApp(simulateFailure: true));
    await tester.pumpAndSettle();

    await _selectMethodAndContinue(
      tester,
      const Key('invoice-payment-method-card'),
    );

    await _confirmPayment(tester);

    expect(
      find.byKey(const Key('invoice-payment-processing')),
      findsOneWidget,
    );
    expect(
      find.byKey(const Key('invoice-payment-result-failure')),
      findsNothing,
    );

    await tester.pump(const Duration(milliseconds: 300));
    await tester.pumpAndSettle();

    final failure = find.byKey(const Key('invoice-payment-result-failure'));
    await _ensurePaymentEntryVisible(tester, failure);
    expect(failure, findsOneWidget);
    expect(find.byKey(const Key('invoice-payment-result-success')), findsNothing);
    expect(find.text('Payment failed'), findsOneWidget);
    expect(
      find.text('The invoice payment could not be completed.'),
      findsOneWidget,
    );
    expect(find.text('Card'), findsOneWidget);
    expect(
      find.byKey(const Key('subscription-invoice-payment-summary')),
      findsOneWidget,
    );
    expect(find.textContaining('invoice-beta125-pending'), findsOneWidget);
  });

  testWidgets('retry returns to payment method selection and clears selection',
      (tester) async {
    await tester.pumpWidget(_paymentApp(simulateFailure: true));
    await tester.pumpAndSettle();

    await _selectMethodAndContinue(
      tester,
      const Key('invoice-payment-method-wallet'),
    );
    await _confirmPayment(tester);
    await tester.pump(const Duration(milliseconds: 300));
    await tester.pumpAndSettle();

    final retry = find.byKey(const Key('invoice-payment-retry'));
    await _ensurePaymentEntryVisible(tester, retry);
    expect(retry, findsOneWidget);
    await tester.tap(retry);
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('invoice-payment-result-failure')),
      findsNothing,
    );
    expect(
      find.byKey(const Key('invoice-payment-method-wallet')),
      findsOneWidget,
    );
    expect(
      find.byKey(const Key('subscription-invoice-payment-summary')),
      findsOneWidget,
    );

    final continueFinder = find.byKey(const Key('invoice-payment-continue'));
    await _ensurePaymentEntryVisible(tester, continueFinder);
    final continueButton = tester.widget<FilledButton>(continueFinder);
    expect(continueButton.onPressed, isNull);
  });

  testWidgets('result exposes done and back-to-invoices actions', (tester) async {
    await tester.pumpWidget(_paymentApp());
    await tester.pumpAndSettle();

    await _selectMethodAndContinue(
      tester,
      const Key('invoice-payment-method-card'),
    );

    await _confirmPayment(tester);
    await tester.pump(const Duration(milliseconds: 300));
    await tester.pumpAndSettle();

    final done = find.byKey(const Key('invoice-payment-done'));
    await _ensurePaymentEntryVisible(tester, done);
    expect(done, findsOneWidget);

    final backToInvoices =
        find.byKey(const Key('invoice-payment-back-to-invoices'));
    await _ensurePaymentEntryVisible(tester, backToInvoices);
    expect(backToInvoices, findsOneWidget);
  });

  testWidgets('French confirmation and result localization is available',
      (tester) async {
    await tester.pumpWidget(_paymentApp(locale: const Locale('fr')));
    await tester.pumpAndSettle();

    await _selectMethodAndContinue(
      tester,
      const Key('invoice-payment-method-wallet'),
    );

    final confirmation = find.byKey(const Key('invoice-payment-confirmation'));
    await _ensurePaymentEntryVisible(tester, confirmation);
    expect(find.textContaining('Confirmer', findRichText: true), findsWidgets);

    await _confirmPayment(tester);
    await tester.pump(const Duration(milliseconds: 300));
    await tester.pumpAndSettle();

    final result = find.byKey(const Key('invoice-payment-result-success'));
    await _ensurePaymentEntryVisible(tester, result);
    expect(result, findsOneWidget);
    expect(find.textContaining('Paiement', findRichText: true), findsWidgets);
  });

  testWidgets('French failure and retry localization is available', (tester) async {
    await tester.pumpWidget(
      _paymentApp(
        locale: const Locale('fr'),
        simulateFailure: true,
      ),
    );
    await tester.pumpAndSettle();

    await _selectMethodAndContinue(
      tester,
      const Key('invoice-payment-method-mobile-money'),
    );
    await _confirmPayment(tester);
    await tester.pump(const Duration(milliseconds: 300));
    await tester.pumpAndSettle();

    final failure = find.byKey(const Key('invoice-payment-result-failure'));
    await _ensurePaymentEntryVisible(tester, failure);
    expect(failure, findsOneWidget);
    expect(find.text('Échec du paiement'), findsOneWidget);
    expect(
      find.text('Le paiement de la facture n’a pas pu être effectué.'),
      findsOneWidget,
    );

    final retry = find.byKey(const Key('invoice-payment-retry'));
    await _ensurePaymentEntryVisible(tester, retry);
    expect(find.text('Réessayer'), findsOneWidget);
  });
}

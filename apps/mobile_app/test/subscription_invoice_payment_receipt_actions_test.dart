import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/pages/subscription_invoice_payment_entry_page.dart';

const _invoice = SubscriptionInvoice(
  id: 'invoice-beta129-pending',
  subscriptionId: 'subscription-beta129',
  amount: 64.50,
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

  for (var i = 0; i < 14 && finder.evaluate().isEmpty; i++) {
    await tester.drag(listView, const Offset(0, -240));
    await tester.pumpAndSettle();
  }

  expect(finder, findsOneWidget);
  await tester.ensureVisible(finder);
  await tester.pumpAndSettle();
}

Future<void> _waitForCopyFeedback(WidgetTester tester) async {
  final feedback = find.byKey(
    const Key('invoice-payment-receipt-copy-feedback'),
  );

  for (var i = 0; i < 20 && feedback.evaluate().isEmpty; i++) {
    await tester.pump(const Duration(milliseconds: 50));
  }

  expect(feedback, findsOneWidget);
}

Future<void> _reachResult(
  WidgetTester tester, {
  Locale locale = const Locale('en'),
  bool simulateFailure = false,
}) async {
  await tester.pumpWidget(
    _app(locale: locale, simulateFailure: simulateFailure),
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
}) async {
  await _reachResult(tester, locale: locale);

  expect(
    find.byKey(const Key('invoice-payment-result-success')),
    findsOneWidget,
  );

  final viewReceipt = find.byKey(const Key('invoice-payment-view-receipt'));
  await _ensureVisible(tester, viewReceipt);
  await tester.tap(viewReceipt);
  await tester.pumpAndSettle();

  final receipt = find.byKey(const Key('invoice-payment-receipt'));
  await _ensureVisible(tester, receipt);
  expect(receipt, findsOneWidget);
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(SystemChannels.platform, (call) async {
      if (call.method == 'Clipboard.setData') {
        return null;
      }
      return null;
    });
  });

  tearDown(() {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(SystemChannels.platform, null);
  });

  testWidgets('receipt copies payment reference and shows feedback',
      (tester) async {
    await _openReceipt(tester);

    final copy = find.byKey(
      const Key('invoice-payment-receipt-copy-reference'),
    );
    await _ensureVisible(tester, copy);
    await tester.tap(copy);
    await _waitForCopyFeedback(tester);

    expect(find.text('Reference copied'), findsOneWidget);
  });

  testWidgets('receipt share preview preserves all core receipt data',
      (tester) async {
    await _openReceipt(tester);

    final share = find.byKey(const Key('invoice-payment-receipt-share'));
    await _ensureVisible(tester, share);
    await tester.tap(share);
    await tester.pumpAndSettle();

    final contentFinder = find.byKey(
      const Key('invoice-payment-receipt-share-content'),
    );
    expect(contentFinder, findsOneWidget);

    final content = tester.widget<SelectableText>(contentFinder).data!;
    expect(content, contains('Payment receipt'));
    expect(content, contains('invoice-beta129-pending'));
    expect(content, contains('64.50'));
    expect(content, contains('EUR'));
    expect(content, contains('AfWal balance'));
    expect(content, contains('BETA-invoice-beta129-pending'));
    expect(content, contains('Payment successful'));
    expect(
      find.text(
        'This beta prepares receipt text locally. Nothing is sent to another app.',
      ),
      findsOneWidget,
    );
  });

  testWidgets('receipt actions are available only after successful receipt',
      (tester) async {
    await _reachResult(tester, simulateFailure: true);

    expect(
      find.byKey(const Key('invoice-payment-result-failure')),
      findsOneWidget,
    );
    expect(
      find.byKey(const Key('invoice-payment-receipt-copy-reference')),
      findsNothing,
    );
    expect(
      find.byKey(const Key('invoice-payment-receipt-share')),
      findsNothing,
    );
    expect(
      find.byKey(const Key('invoice-payment-receipt-share-content')),
      findsNothing,
    );
  });

  testWidgets('French receipt actions and feedback are localized',
      (tester) async {
    await _openReceipt(tester, locale: const Locale('fr'));

    final copy = find.byKey(
      const Key('invoice-payment-receipt-copy-reference'),
    );
    await _ensureVisible(tester, copy);
    expect(find.text('Copier la référence'), findsOneWidget);
    expect(find.text('Partager le reçu'), findsOneWidget);

    await tester.tap(copy);
    await _waitForCopyFeedback(tester);
    expect(find.text('Référence copiée'), findsOneWidget);

    await tester.pump(const Duration(seconds: 5));

    final share = find.byKey(const Key('invoice-payment-receipt-share'));
    await tester.tap(share);
    await tester.pumpAndSettle();

    expect(find.text('Aperçu du partage du reçu'), findsOneWidget);
    final content = tester.widget<SelectableText>(
      find.byKey(const Key('invoice-payment-receipt-share-content')),
    ).data!;
    expect(content, contains('Reçu de paiement'));
    expect(content, contains('Paiement réussi'));
    expect(content, contains('Solde AfWal'));
  });
}

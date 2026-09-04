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

Widget _paymentApp({Locale locale = const Locale('en')}) => MaterialApp(
      locale: locale,
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: const SubscriptionInvoicePaymentEntryPage(
        invoice: _pendingInvoice,
      ),
    );

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
    expect(find.text('Invoice already paid'), findsOneWidget);

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

    final continueButton = tester.widget<FilledButton>(
      find.byKey(const Key('invoice-payment-continue')),
    );

    expect(continueButton.onPressed, isNull);
    expect(
      find.byKey(const Key('invoice-payment-confirmation-preview')),
      findsNothing,
    );
  });

  testWidgets('wallet selection reveals read-only confirmation preview',
      (tester) async {
    await tester.pumpWidget(_paymentApp());
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('invoice-payment-method-wallet')));
    await tester.pumpAndSettle();

    final continueButton = tester.widget<FilledButton>(
      find.byKey(const Key('invoice-payment-continue')),
    );
    expect(continueButton.onPressed, isNotNull);

    await tester.tap(find.byKey(const Key('invoice-payment-continue')));
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('invoice-payment-confirmation-preview')),
      findsOneWidget,
    );
    expect(
      find.byKey(const Key('invoice-payment-confirm-read-only')),
      findsOneWidget,
    );
  });

  testWidgets('mobile money and card are selectable local placeholders',
      (tester) async {
    for (final key in [
      const Key('invoice-payment-method-mobile-money'),
      const Key('invoice-payment-method-card'),
    ]) {
      await tester.pumpWidget(_paymentApp());
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(key));
      await tester.pumpAndSettle();

      final continueButton = tester.widget<FilledButton>(
        find.byKey(const Key('invoice-payment-continue')),
      );
      expect(continueButton.onPressed, isNotNull);
    }
  });

  testWidgets('confirmation action remains explicitly read-only', (tester) async {
    await tester.pumpWidget(_paymentApp());
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('invoice-payment-method-wallet')));
    await tester.tap(find.byKey(const Key('invoice-payment-continue')));
    await tester.pumpAndSettle();

    await tester.tap(
      find.byKey(const Key('invoice-payment-confirm-read-only')),
    );
    await tester.pump();

    expect(find.byType(SnackBar), findsOneWidget);
    expect(
      find.byKey(const Key('subscription-invoice-payment-entry-page')),
      findsOneWidget,
    );
  });

  testWidgets('French payment entry localization is available', (tester) async {
    await tester.pumpWidget(_paymentApp(locale: const Locale('fr')));
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('subscription-invoice-payment-entry-page')),
      findsOneWidget,
    );
    expect(find.textContaining('paiement', findRichText: true), findsWidgets);
  });
}

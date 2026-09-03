import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/pages/subscription_invoice_detail_page.dart';

const _invoice = SubscriptionInvoice(
  id: 'invoice-beta18-1',
  subscriptionId: 'subscription-beta18',
  amount: 24.99,
  currency: 'EUR',
  status: 'PAID',
  issueDate: '2026-09-03',
);

Widget _app({VoidCallback? onReturn}) => MaterialApp(
      locale: const Locale('fr'),
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: SubscriptionInvoiceDetailPage(
        invoice: _invoice,
        onReturnToBillingHistory: onReturn,
      ),
    );

void main() {
  testWidgets('invoice detail displays read-only invoice data', (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('subscription-invoice-detail-page')), findsOneWidget);
    expect(find.textContaining('invoice-beta18-1'), findsOneWidget);
    expect(find.textContaining('PAID'), findsOneWidget);
    expect(find.textContaining('2026-09-03'), findsOneWidget);
    expect(find.textContaining('24,99'), findsOneWidget);
    expect(find.textContaining('EUR'), findsOneWidget);
  });

  testWidgets('invoice detail returns explicitly to billing history', (tester) async {
    var returns = 0;
    await tester.pumpWidget(_app(onReturn: () => returns += 1));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('subscription-invoice-detail-return-history')));
    expect(returns, 1);
  });
}

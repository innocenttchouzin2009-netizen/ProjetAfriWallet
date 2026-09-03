import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/presentation/subscription_invoice_status_presentation.dart';

Widget _app(Locale locale) => MaterialApp(
      locale: locale,
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: Builder(
        builder: (context) {
          final localizations = AppLocalizations.of(context)!;
          final labels = [
            localizeSubscriptionInvoiceStatus(localizations, 'DRAFT'),
            localizeSubscriptionInvoiceStatus(localizations, 'PENDING'),
            localizeSubscriptionInvoiceStatus(localizations, 'PAID'),
            localizeSubscriptionInvoiceStatus(localizations, 'FAILED'),
            localizeSubscriptionInvoiceStatus(localizations, 'OVERDUE'),
            localizeSubscriptionInvoiceStatus(localizations, 'CANCELLED'),
            localizeSubscriptionInvoiceStatus(localizations, 'UNKNOWN'),
          ];
          return Text(labels.join('|'));
        },
      ),
    );

void main() {
  testWidgets('invoice status presentation localizes all supported statuses in French', (tester) async {
    await tester.pumpWidget(_app(const Locale('fr')));
    await tester.pumpAndSettle();

    expect(
      find.text('Brouillon|En attente|Payée|Échouée|En retard|Annulée|UNKNOWN'),
      findsOneWidget,
    );
  });

  testWidgets('invoice status presentation localizes all supported statuses in English', (tester) async {
    await tester.pumpWidget(_app(const Locale('en')));
    await tester.pumpAndSettle();

    expect(
      find.text('Draft|Pending|Paid|Failed|Overdue|Cancelled|UNKNOWN'),
      findsOneWidget,
    );
  });
}

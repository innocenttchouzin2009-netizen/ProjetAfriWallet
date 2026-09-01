import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/pages/subscription_offer_detail_page.dart';

const _offer = SubscriptionOffer(
  id: 'offer-beta14',
  providerId: 'provider-beta14',
  name: 'Premium Beta14',
  description: 'Description courte',
  price: 12.5,
  currency: 'EUR',
  country: 'DE',
  category: 'streaming',
  features: ['HD', 'Multi-device'],
  longDescription: 'Description longue Beta1.14',
);

Widget _buildApp({VoidCallback? onContinue}) {
  return MaterialApp(
    locale: const Locale('fr'),
    supportedLocales: AppLocalizations.supportedLocales,
    localizationsDelegates: AppLocalizations.localizationsDelegates,
    home: SubscriptionOfferDetailPage(
      offer: _offer,
      onContinue: onContinue,
    ),
  );
}

void main() {
  testWidgets('offer detail renders existing offer data', (tester) async {
    await tester.pumpWidget(_buildApp());
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('subscription-offer-detail-page')), findsOneWidget);
    expect(find.byKey(const Key('subscription-offer-detail-title')), findsOneWidget);
    expect(find.text('Premium Beta14'), findsNWidgets(2));
    expect(find.text('Description longue Beta1.14'), findsOneWidget);
    expect(find.text('provider-beta14'), findsOneWidget);
    expect(find.text('DE'), findsOneWidget);
    expect(find.text('streaming'), findsOneWidget);
    expect(find.text('HD'), findsOneWidget);
    expect(find.text('Multi-device'), findsOneWidget);
    expect(find.byKey(const Key('subscription-offer-detail-continue')), findsNothing);
  });

  testWidgets('continue opens confirmation without firing callback', (tester) async {
    var continueCount = 0;

    await tester.pumpWidget(_buildApp(onContinue: () => continueCount += 1));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('subscription-offer-detail-continue')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('subscription-offer-confirmation-dialog')), findsOneWidget);
    expect(find.byKey(const Key('subscription-offer-confirmation-description')), findsOneWidget);
    expect(find.byKey(const Key('subscription-offer-confirmation-price')), findsOneWidget);
    expect(continueCount, 0);
  });

  testWidgets('cancel closes confirmation without firing callback', (tester) async {
    var continueCount = 0;

    await tester.pumpWidget(_buildApp(onContinue: () => continueCount += 1));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('subscription-offer-detail-continue')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('subscription-offer-confirmation-cancel')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('subscription-offer-confirmation-dialog')), findsNothing);
    expect(continueCount, 0);
  });

  testWidgets('confirm fires callback exactly once and closes dialog', (tester) async {
    var continueCount = 0;

    await tester.pumpWidget(_buildApp(onContinue: () => continueCount += 1));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('subscription-offer-detail-continue')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('subscription-offer-confirmation-confirm')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('subscription-offer-confirmation-dialog')), findsNothing);
    expect(continueCount, 1);
  });
}

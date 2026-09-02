import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/pages/subscription_activation_result_page.dart';

const _offer = SubscriptionOffer(
  id: 'offer-beta15',
  providerId: 'provider-beta15',
  name: 'Beta15 Premium',
  description: 'Beta1.15 offer',
  price: 14.99,
  currency: 'EUR',
  country: 'DE',
  category: 'Entertainment',
  features: ['Feature A'],
  longDescription: 'Beta1.15 activation result description',
);

Widget _app({VoidCallback? onReturnToSubscriptions, VoidCallback? onReturnToWallet}) {
  return MaterialApp(
    locale: const Locale('fr'),
    supportedLocales: AppLocalizations.supportedLocales,
    localizationsDelegates: AppLocalizations.localizationsDelegates,
    home: SubscriptionActivationResultPage(
      offer: _offer,
      onReturnToSubscriptions: onReturnToSubscriptions,
      onReturnToWallet: onReturnToWallet,
    ),
  );
}

void main() {
  testWidgets('activation result renders existing offer data', (tester) async {
    await tester.pumpWidget(_app());
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('subscription-activation-result-page')), findsOneWidget);
    expect(find.text('Beta15 Premium'), findsWidgets);
    expect(find.text('Beta1.15 activation result description'), findsOneWidget);
    expect(find.byKey(const Key('subscription-activation-result-price')), findsOneWidget);
    expect(find.text('provider-beta15'), findsOneWidget);
  });

  testWidgets('activation result exposes subscriptions callback when provided', (tester) async {
    var calls = 0;
    await tester.pumpWidget(_app(onReturnToSubscriptions: () => calls += 1));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('subscription-activation-result-return-subscriptions')));
    expect(calls, 1);
    expect(find.byKey(const Key('subscription-activation-result-return-wallet')), findsNothing);
  });

  testWidgets('activation result exposes wallet callback when provided', (tester) async {
    var calls = 0;
    await tester.pumpWidget(_app(onReturnToWallet: () => calls += 1));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('subscription-activation-result-return-wallet')));
    expect(calls, 1);
    expect(find.byKey(const Key('subscription-activation-result-return-subscriptions')), findsNothing);
  });
}

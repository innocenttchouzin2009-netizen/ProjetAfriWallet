import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/pages/subscription_detail_page.dart';

const _subscription = UserSubscription(
  id: 'subscription-beta16',
  offerId: 'offer-beta16',
  providerId: 'provider-beta16',
  name: 'Beta16 Premium',
  status: 'ACTIVE',
  autoRenew: true,
  nextBillingDate: '2026-10-02',
  currentCycle: 'Cycle 2',
  price: 14.99,
  currency: 'EUR',
);

Widget _app({VoidCallback? onReturnToSubscriptions, VoidCallback? onReturnToWallet}) {
  return MaterialApp(
    locale: const Locale('fr'),
    supportedLocales: AppLocalizations.supportedLocales,
    localizationsDelegates: AppLocalizations.localizationsDelegates,
    home: SubscriptionDetailPage(
      subscription: _subscription,
      onReturnToSubscriptions: onReturnToSubscriptions,
      onReturnToWallet: onReturnToWallet,
    ),
  );
}

void main() {
  testWidgets('subscription detail displays existing subscription state', (tester) async {
    await tester.pumpWidget(_app());
    expect(find.byKey(const Key('subscription-detail-page')), findsOneWidget);
    expect(find.text('Beta16 Premium'), findsWidgets);
    expect(find.text('ACTIVE'), findsOneWidget);
    expect(find.text('provider-beta16'), findsOneWidget);
    expect(find.text('Cycle 2'), findsOneWidget);
    expect(find.text('2026-10-02'), findsOneWidget);
    expect(find.textContaining('14'), findsWidgets);
  });

  testWidgets('auto renew is read only in Beta1.16', (tester) async {
    await tester.pumpWidget(_app());
    final switchTile = tester.widget<SwitchListTile>(find.byKey(const Key('subscription-detail-auto-renew-readonly')));
    expect(switchTile.value, isTrue);
    expect(switchTile.onChanged, isNull);
  });

  testWidgets('detail returns to subscriptions through explicit callback', (tester) async {
    var returns = 0;
    await tester.pumpWidget(_app(onReturnToSubscriptions: () => returns += 1));
    await tester.tap(find.byKey(const Key('subscription-detail-return-subscriptions')));
    expect(returns, 1);
  });

  testWidgets('wallet return is exposed only when wallet context exists', (tester) async {
    var returns = 0;
    await tester.pumpWidget(_app(onReturnToWallet: () => returns += 1));
    expect(find.byKey(const Key('subscription-detail-return-wallet')), findsOneWidget);
    await tester.tap(find.byKey(const Key('subscription-detail-return-wallet')));
    expect(returns, 1);
  });
}

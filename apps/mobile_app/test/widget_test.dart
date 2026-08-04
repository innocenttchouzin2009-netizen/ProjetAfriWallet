// This is a basic Flutter widget test.
//
// To perform an interaction with a widget in your test, use the WidgetTester
// utility in the flutter_test package. For example, you can send tap and scroll
// gestures. You can also use WidgetTester to find child widgets in the widget
// tree, read text, and verify that the values of widget properties are correct.

import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/main.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/services/subscription_repository.dart';

void main() {
  testWidgets('subscriptions page renders', (WidgetTester tester) async {
    await tester.pumpWidget(AfriWalletApp(repository: _FakeSubscriptionRepository()));
    await tester.pumpAndSettle();

    expect(find.text('Abonnements'), findsOneWidget);
    expect(find.text('Mes abonnements'), findsOneWidget);
  });
}

class _FakeSubscriptionRepository implements SubscriptionRepository {
  @override
  Future<List<SubscriptionOffer>> fetchOffers({String? country, String? currency, String? query}) async {
    return [];
  }

  @override
  Future<SubscriptionOffer?> fetchOffer(String offerId) async {
    return null;
  }

  @override
  Future<List<UserSubscription>> fetchUserSubscriptions() async {
    return [];
  }

  @override
  Future<List<SubscriptionInvoice>> fetchInvoices(String subscriptionId) async {
    return [];
  }

  @override
  Future<void> createSubscription(String offerId) async {}

  @override
  Future<void> cancelSubscription(String subscriptionId) async {}

  @override
  Future<void> toggleAutoRenew(String subscriptionId, bool enabled) async {}
}

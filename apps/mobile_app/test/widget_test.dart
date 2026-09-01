import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:mobile_app/main.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/services/subscription_repository.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('app renders Mobile Beta welcome experience', (WidgetTester tester) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(AfriWalletApp(repository: _FakeSubscriptionRepository()));
    await tester.pumpAndSettle();

    expect(find.text('MOBILE BETA 1'), findsOneWidget);
    expect(find.text('Une identité.\nUn wallet.\nUne Afrique connectée.'), findsOneWidget);
    expect(find.text('Découvrir AfWal'), findsOneWidget);
    expect(find.text('Connecting Africa. Empowering People.'), findsOneWidget);
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

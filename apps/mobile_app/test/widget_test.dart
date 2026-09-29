import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/main.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/services/auth_production_wiring.dart';
import 'package:mobile_app/services/secure_storage_adapter.dart';
import 'package:mobile_app/services/subscription_repository.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  testWidgets(
    'app renders Mobile Beta welcome experience before auth restoration',
    (WidgetTester tester) async {
      SharedPreferences.setMockInitialValues({});
      final wiring = _testAuthWiring();
      addTearDown(wiring.dispose);

      await tester.pumpWidget(
        AfriWalletApp(
          repository: _FakeSubscriptionRepository(),
          authProductionWiring: wiring,
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('MOBILE BETA 1'), findsOneWidget);
      expect(
        find.text('Une identité.\nUn wallet.\nUne Afrique connectée.'),
        findsOneWidget,
      );
      expect(find.text('Découvrir AfWal'), findsOneWidget);
      expect(
        find.text('Connecting Africa. Empowering People.'),
        findsOneWidget,
      );
    },
  );

  testWidgets(
    'auth bootstrap fails closed to onboarding when no session is stored',
    (WidgetTester tester) async {
      SharedPreferences.setMockInitialValues({});
      var networkCalls = 0;
      final wiring = _testAuthWiring(
        onRequest: (_) async {
          networkCalls += 1;
          return http.Response('', 500);
        },
      );
      addTearDown(wiring.dispose);

      await tester.pumpWidget(
        AfriWalletApp(
          repository: _FakeSubscriptionRepository(),
          authProductionWiring: wiring,
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.text('Découvrir AfWal'));
      await tester.pumpAndSettle();

      expect(
        find.text('Une identité pour une Afrique connectée'),
        findsOneWidget,
      );
      expect(find.text('AfWal ID indisponible'), findsNothing);
      expect(networkCalls, 0);
    },
  );

  testWidgets(
    'auth bootstrap restores stored session before entering app experience',
    (WidgetTester tester) async {
      SharedPreferences.setMockInitialValues({});
      var networkCalls = 0;
      final wiring = _testAuthWiring(
        onRequest: (_) async {
          networkCalls += 1;
          return http.Response('', 500);
        },
      );
      addTearDown(wiring.dispose);

      await wiring.sessionStore.save(
        StoredAuthSession(
          accessToken: 'stored-access',
          refreshToken: 'stored-refresh',
          tokenType: 'Bearer',
          sessionId: 'session-1',
          userId: 'user-1',
          accessTokenExpiresAtUtc:
              DateTime.now().toUtc().add(const Duration(hours: 1)),
        ),
      );

      await tester.pumpWidget(
        AfriWalletApp(
          repository: _FakeSubscriptionRepository(),
          authProductionWiring: wiring,
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.text('Découvrir AfWal'));
      await tester.pumpAndSettle();

      expect(find.text('Mon AfWal ID'), findsOneWidget);
      expect(find.text('AfWal ID indisponible'), findsOneWidget);
      expect(
        find.text('Une identité pour une Afrique connectée'),
        findsNothing,
      );
      expect(networkCalls, 0);
    },
  );
}

AuthProductionWiring _testAuthWiring({
  Future<http.Response> Function(http.Request request)? onRequest,
}) {
  final storage = _MemorySecureStorageAdapter();
  final apiClient = ApiClient(
    baseUrl: 'https://api.afwal.test',
    httpClient: MockClient(
      onRequest ?? (_) async => http.Response('', 500),
    ),
  );

  return AuthProductionWiring(
    apiClient: apiClient,
    secureStorageAdapter: storage,
  );
}

class _MemorySecureStorageAdapter implements SecureStorageAdapter {
  final Map<String, String> _values = <String, String>{};

  @override
  Future<void> write({
    required String key,
    required String value,
  }) async {
    _values[key] = value;
  }

  @override
  Future<String?> read({required String key}) async => _values[key];

  @override
  Future<void> delete({required String key}) async {
    _values.remove(key);
  }
}

class _FakeSubscriptionRepository implements SubscriptionRepository {
  @override
  Future<List<SubscriptionOffer>> fetchOffers({
    String? country,
    String? currency,
    String? query,
  }) async {
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
  Future<List<SubscriptionInvoice>> fetchInvoices(
    String subscriptionId,
  ) async {
    return [];
  }

  @override
  Future<void> createSubscription(String offerId) async {}

  @override
  Future<void> cancelSubscription(String subscriptionId) async {}

  @override
  Future<void> toggleAutoRenew(
    String subscriptionId,
    bool enabled,
  ) async {}
}

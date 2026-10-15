import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/main.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/models/current_identity_profile.dart';
import 'package:mobile_app/models/payment_transfer.dart';
import 'package:mobile_app/models/public_afwal_identity.dart';
import 'package:mobile_app/models/transaction_history.dart';
import 'package:mobile_app/models/wallet_balance.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/services/auth_production_wiring.dart';
import 'package:mobile_app/services/identity_read_repository.dart';
import 'package:mobile_app/services/secure_storage_adapter.dart';
import 'package:mobile_app/services/subscription_repository.dart';
import 'package:mobile_app/services/transaction_history_repository.dart';
import 'package:mobile_app/services/transfer_repository.dart';
import 'package:mobile_app/services/wallet_repository.dart';
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

  testWidgets(
    'premium wallet home receives authoritative AfWal ID from identity handoff',
    (WidgetTester tester) async {
      SharedPreferences.setMockInitialValues({});
      final wiring = _testAuthWiring();
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
          identityReadRepository: _ReadyIdentityReadRepository(),
          walletRepository: _ReadyWalletRepository(),
          transferRepository: _ReadyTransferRepository(),
          transactionHistoryRepository: _ReadyTimelineRepository(),
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.text('Découvrir AfWal'));
      await tester.pumpAndSettle();

      expect(find.text('@premium-user'), findsOneWidget);
      await tester.tap(find.text('Continuer'));
      await tester.pumpAndSettle();

      expect(find.text('Wallet Home'), findsOneWidget);
      expect(find.byKey(const Key('wallet-home-afwal-id')), findsOneWidget);
      expect(find.text('@premium-user'), findsOneWidget);
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


class _ReadyIdentityReadRepository implements IdentityReadRepository {
  @override
  Future<CurrentIdentityProfile> loadCurrentProfile() async {
    return CurrentIdentityProfile(
      userId: 'user-1',
      identifier: 'user@example.com',
      createdAtUtc: DateTime.utc(2026, 10, 15, 10),
    );
  }

  @override
  Future<PublicAfWalIdentity> loadPublicAfWalId(String afWalId) async {
    return PublicAfWalIdentity(afWalId: afWalId);
  }
}

class _ReadyTransferRepository implements TransferRepository {
  @override
  Future<ReceiveIdentity> loadReceiveIdentity() async {
    return const ReceiveIdentity(
      publicLabel: '@premium-user',
      qrToken: 'premium-qr-token',
    );
  }

  @override
  Future<TransferReceipt> send(SendTransferRequest request) {
    throw UnimplementedError();
  }
}

class _ReadyWalletRepository implements WalletRepository {
  @override
  Future<List<WalletBalance>> loadWalletBalances() async {
    return const <WalletBalance>[
      WalletBalance(
        walletId: 'wallet-premium-eur',
        currency: 'EUR',
        availableMinor: 12345,
        status: 'ACTIVE',
        countryCode: 'DE',
      ),
    ];
  }
}

class _ReadyTimelineRepository implements TransactionHistoryRepository {
  @override
  Future<List<TransactionHistoryItem>> listTransactions() async {
    return const <TransactionHistoryItem>[];
  }
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

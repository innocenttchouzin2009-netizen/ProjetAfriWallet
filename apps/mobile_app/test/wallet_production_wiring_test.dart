import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/services/secure_session_store.dart';
import 'package:mobile_app/services/wallet_production_wiring.dart';

void main() {
  test('production wallet wiring composes authenticated remote wallet access', () async {
    late http.Request captured;
    final apiClient = ApiClient(
      baseUrl: 'https://api.afwal.test',
      httpClient: MockClient((request) async {
        captured = request;
        return http.Response(
          jsonEncode(<String, Object?>{
            'wallets': <Object?>[
              <String, Object?>{
                'walletId': '11111111-1111-1111-1111-111111111111',
                'currency': 'XAF',
                'availableMinor': 125000,
                'status': 'ACTIVE',
                'countryCode': 'CM',
              },
            ],
          }),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        );
      }),
    );
    final wiring = WalletProductionWiring(
      sessionStore: _MemoryAuthSessionStore(_storedSession()),
      apiClient: apiClient,
    );

    final wallets = await wiring.repository.loadWalletBalances();

    expect(captured.method, 'GET');
    expect(captured.url.path, '/api/v1/wallets/mobile');
    expect(captured.headers['Authorization'], 'Bearer access-secret');
    expect(wallets, hasLength(1));
    expect(wallets.single.currency, 'XAF');

    wiring.dispose();
    apiClient.close();
  });
}

StoredAuthSession _storedSession() => StoredAuthSession(
      accessToken: 'access-secret',
      refreshToken: 'refresh-secret',
      tokenType: 'Bearer',
      sessionId: 'session-1',
      userId: 'user-1',
      accessTokenExpiresAtUtc: DateTime.utc(2026, 9, 27, 14),
    );

class _MemoryAuthSessionStore implements AuthSessionStore {
  _MemoryAuthSessionStore(this.session);

  StoredAuthSession? session;

  @override
  Future<StoredAuthSession?> read() async => session;

  @override
  Future<void> save(StoredAuthSession session) async {
    this.session = session;
  }

  @override
  Future<void> clear() async {
    session = null;
  }
}

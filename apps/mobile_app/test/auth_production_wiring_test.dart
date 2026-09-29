import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/models/auth_requests.dart';
import 'package:mobile_app/models/auth_session.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/services/auth_production_wiring.dart';
import 'package:mobile_app/services/auth_state_controller.dart';
import 'package:mobile_app/services/secure_storage_adapter.dart';

void main() {
  test('production wiring shares one secure session store across auth services',
      () async {
    final storage = _MemorySecureStorageAdapter();
    final apiClient = ApiClient(
      baseUrl: 'https://api.example.test',
      httpClient: MockClient((request) async {
        expect(request.method, 'POST');
        expect(request.url.path, '/api/v1/auth/login');
        return http.Response(
          jsonEncode(<String, Object?>{
            'accessToken': 'access-token',
            'refreshToken': 'refresh-token',
            'tokenType': 'Bearer',
            'expiresIn': 900,
            'sessionId': 'session-1',
            'userId': 'user-1',
          }),
          200,
        );
      }),
    );
    final wiring = AuthProductionWiring(
      apiClient: apiClient,
      secureStorageAdapter: storage,
    );
    addTearDown(() {
      wiring.dispose();
      apiClient.close();
    });

    final session = await wiring.repository.login(
      const AuthLoginRequest(
        identifier: 'user@example.com',
        password: 'secret',
        deviceId: 'device-1',
        platform: 'android',
        deviceName: 'Pixel',
      ),
    );

    expect(session.accessToken, 'access-token');
    expect((await wiring.sessionStore.read())?.sessionId, 'session-1');

    await wiring.stateController.restore();

    expect(
      wiring.stateController.state.status,
      AuthStateStatus.authenticated,
    );
    expect(wiring.stateController.state.session?.userId, 'user-1');
  });

  test('restoring a valid stored session does not call the network', () async {
    var networkCalls = 0;
    final storage = _MemorySecureStorageAdapter();
    final apiClient = ApiClient(
      baseUrl: 'https://api.example.test',
      httpClient: MockClient((request) async {
        networkCalls += 1;
        return http.Response('', 500);
      }),
    );
    final wiring = AuthProductionWiring(
      apiClient: apiClient,
      secureStorageAdapter: storage,
    );
    addTearDown(() {
      wiring.dispose();
      apiClient.close();
    });

    await wiring.sessionStore.save(
      StoredAuthSession(
        accessToken: 'stored-access',
        refreshToken: 'stored-refresh',
        tokenType: 'Bearer',
        sessionId: 'session-2',
        userId: 'user-2',
        accessTokenExpiresAtUtc:
            DateTime.now().toUtc().add(const Duration(hours: 1)),
      ),
    );

    await wiring.stateController.restore();

    expect(networkCalls, 0);
    expect(
      wiring.stateController.state.status,
      AuthStateStatus.authenticated,
    );
    expect(wiring.stateController.state.session?.sessionId, 'session-2');
  });

  test('clearing auth state clears the shared secure session store', () async {
    final storage = _MemorySecureStorageAdapter();
    final apiClient = ApiClient(
      baseUrl: 'https://api.example.test',
      httpClient: MockClient((request) async => http.Response('', 500)),
    );
    final wiring = AuthProductionWiring(
      apiClient: apiClient,
      secureStorageAdapter: storage,
    );
    addTearDown(() {
      wiring.dispose();
      apiClient.close();
    });

    await wiring.sessionStore.save(
      StoredAuthSession(
        accessToken: 'stored-access',
        refreshToken: 'stored-refresh',
        tokenType: 'Bearer',
        sessionId: 'session-3',
        userId: 'user-3',
        accessTokenExpiresAtUtc:
            DateTime.now().toUtc().add(const Duration(hours: 1)),
      ),
    );

    await wiring.stateController.restore();
    await wiring.stateController.clearLocalSession();

    expect(await wiring.sessionStore.read(), isNull);
    expect(
      wiring.stateController.state.status,
      AuthStateStatus.unauthenticated,
    );
  });
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

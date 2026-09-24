import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/models/auth_error.dart';
import 'package:mobile_app/models/auth_requests.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/services/auth_remote_data_source.dart';
import 'package:mobile_app/services/auth_repository.dart';

void main() {
  const baseUrl = 'https://identity.example.test';

  test('login matches backend contract and parses AuthSessionResponse', () async {
    late http.Request captured;

    final client = ApiClient(
      baseUrl: baseUrl,
      httpClient: MockClient((request) async {
        captured = request;
        return http.Response(
          jsonEncode(<String, Object?>{
            'accessToken': 'access-token',
            'refreshToken': 'refresh-token',
            'tokenType': 'Bearer',
            'expiresIn': 900,
            'sessionId': '9dbdc9d3-2b4a-44cc-8bc5-a82578c8d724',
            'userId': '72317790-a921-4d0a-94a8-75d7d36c5064',
          }),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        );
      }),
    );
    final source = AuthRemoteDataSource(client);

    final session = await source.login(
      const AuthLoginRequest(
        identifier: 'user@example.com',
        password: 'secret',
        deviceId: 'device-123',
        platform: 'android',
        deviceName: 'Pixel',
      ),
    );

    expect(captured.method, 'POST');
    expect(captured.url.path, '/api/v1/auth/login');
    expect(jsonDecode(captured.body), <String, Object?>{
      'identifier': 'user@example.com',
      'password': 'secret',
      'deviceId': 'device-123',
      'platform': 'android',
      'deviceName': 'Pixel',
    });
    expect(session.accessToken, 'access-token');
    expect(session.refreshToken, 'refresh-token');
    expect(session.expiresIn, 900);
    client.close();
  });

  test('refresh matches backend contract', () async {
    late http.Request captured;
    final client = ApiClient(
      baseUrl: baseUrl,
      httpClient: MockClient((request) async {
        captured = request;
        return http.Response(
          jsonEncode(<String, Object?>{
            'accessToken': 'access-2',
            'refreshToken': 'refresh-2',
            'tokenType': 'Bearer',
            'expiresIn': 900,
            'sessionId': 'session-2',
            'userId': 'user-2',
          }),
          200,
        );
      }),
    );

    await AuthRemoteDataSource(client).refresh(
      const AuthRefreshRequest(refreshToken: 'refresh-1'),
    );

    expect(captured.url.path, '/api/v1/auth/refresh');
    expect(jsonDecode(captured.body), <String, Object?>{
      'refreshToken': 'refresh-1',
    });
    client.close();
  });

  test('current session sends bearer token and parses backend response', () async {
    late http.Request captured;
    final client = ApiClient(
      baseUrl: baseUrl,
      httpClient: MockClient((request) async {
        captured = request;
        return http.Response(
          jsonEncode(<String, Object?>{
            'userId': 'user-1',
            'sessionId': 'session-1',
            'deviceId': 'device-123',
            'createdAtUtc': '2026-09-24T08:00:00Z',
            'expiresAtUtc': '2026-10-24T08:00:00Z',
            'tokenVersion': 3,
          }),
          200,
        );
      }),
    );

    final session = await AuthRemoteDataSource(client)
        .loadCurrentSession('access-token');

    expect(captured.method, 'GET');
    expect(captured.url.path, '/api/v1/auth/session');
    expect(captured.headers['Authorization'], 'Bearer access-token');
    expect(session.deviceId, 'device-123');
    expect(session.createdAtUtc.isUtc, isTrue);
    expect(session.tokenVersion, 3);
    client.close();
  });

  test('logout and logout-all use protected backend routes', () async {
    final requests = <http.Request>[];
    final client = ApiClient(
      baseUrl: baseUrl,
      httpClient: MockClient((request) async {
        requests.add(request);
        return http.Response('', 204);
      }),
    );
    final source = AuthRemoteDataSource(client);

    await source.logout('access-token');
    await source.logoutAll('access-token');

    expect(
      requests.map((request) => request.url.path),
      <String>['/api/v1/auth/logout', '/api/v1/auth/logout-all'],
    );
    expect(
      requests.every(
        (request) =>
            request.headers['Authorization'] == 'Bearer access-token',
      ),
      isTrue,
    );
    client.close();
  });

  test('repository maps stable backend auth error contract', () async {
    final client = ApiClient(
      baseUrl: baseUrl,
      httpClient: MockClient((request) async {
        return http.Response(
          jsonEncode(<String, Object?>{
            'code': 'AUTH_INVALID_CREDENTIALS',
            'message': 'Invalid credentials.',
            'traceId': 'trace-123',
          }),
          401,
        );
      }),
    );
    final repository = RemoteAuthRepository(AuthRemoteDataSource(client));

    expect(
      repository.login(
        const AuthLoginRequest(
          identifier: 'user@example.com',
          password: 'wrong',
          deviceId: 'device-123',
          platform: 'android',
        ),
      ),
      throwsA(
        isA<AuthRepositoryException>().having(
          (error) => error.error.code,
          'error code',
          AuthErrorCode.invalidCredentials,
        ),
      ),
    );
    client.close();
  });

  test('repository rejects malformed successful auth payload', () async {
    final client = ApiClient(
      baseUrl: baseUrl,
      httpClient: MockClient((request) async {
        return http.Response(
          jsonEncode(<String, Object?>{
            'accessToken': 'access-token',
            'expiresIn': 900,
          }),
          200,
        );
      }),
    );
    final repository = RemoteAuthRepository(AuthRemoteDataSource(client));

    expect(
      repository.login(
        const AuthLoginRequest(
          identifier: 'user@example.com',
          password: 'secret',
          deviceId: 'device-123',
          platform: 'android',
        ),
      ),
      throwsA(
        isA<AuthRepositoryException>().having(
          (error) => error.error.code,
          'error code',
          AuthErrorCode.unknown,
        ),
      ),
    );
    client.close();
  });
}

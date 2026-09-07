import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/auth_remote_data_source.dart';
import 'package:mobile_app/models/auth_credentials.dart';
import 'package:mobile_app/models/auth_error.dart';
import 'package:mobile_app/network/api_client.dart';

void main() {
  const credentials = AuthCredentials(
    identifier: '+237600000000',
    password: 'correct horse battery staple',
    deviceId: 'device-123',
    platform: 'android',
    deviceName: 'Pixel Test',
  );

  Map<String, Object?> sessionPayload({
    String accessToken = 'access-token',
    String refreshToken = 'refresh-token',
  }) => <String, Object?>{
    'accessToken': accessToken,
    'refreshToken': refreshToken,
    'tokenType': 'Bearer',
    'expiresIn': 900,
    'sessionId': '11111111-1111-1111-1111-111111111111',
    'userId': '22222222-2222-2222-2222-222222222222',
  };

  test('login matches the backend request and response contract', () async {
    final client = MockClient((request) async {
      expect(request.method, 'POST');
      expect(request.url.path, ApiAuthRemoteDataSource.loginPath);
      expect(request.headers['content-type'], 'application/json');
      expect(
        jsonDecode(request.body),
        <String, Object?>{
          'identifier': credentials.identifier,
          'password': credentials.password,
          'deviceId': credentials.deviceId,
          'platform': credentials.platform,
          'deviceName': credentials.deviceName,
        },
      );

      return http.Response(
        jsonEncode(sessionPayload()),
        200,
        headers: <String, String>{'content-type': 'application/json'},
      );
    });
    final apiClient = ApiClient(baseUrl: 'https://identity.test', httpClient: client);
    addTearDown(apiClient.close);
    final source = ApiAuthRemoteDataSource(apiClient: apiClient);

    final session = await source.login(credentials);

    expect(session.accessToken, 'access-token');
    expect(session.refreshToken, 'refresh-token');
    expect(session.tokenType, 'Bearer');
    expect(session.expiresIn, 900);
    expect(session.sessionId, '11111111-1111-1111-1111-111111111111');
    expect(session.userId, '22222222-2222-2222-2222-222222222222');
  });

  test('refresh sends only the refresh token and parses rotation response', () async {
    final client = MockClient((request) async {
      expect(request.method, 'POST');
      expect(request.url.path, ApiAuthRemoteDataSource.refreshPath);
      expect(
        jsonDecode(request.body),
        <String, Object?>{'refreshToken': 'old-refresh-token'},
      );

      return http.Response(
        jsonEncode(
          sessionPayload(
            accessToken: 'rotated-access-token',
            refreshToken: 'rotated-refresh-token',
          ),
        ),
        200,
        headers: <String, String>{'content-type': 'application/json'},
      );
    });
    final apiClient = ApiClient(baseUrl: 'https://identity.test', httpClient: client);
    addTearDown(apiClient.close);
    final source = ApiAuthRemoteDataSource(apiClient: apiClient);

    final session = await source.refresh('old-refresh-token');

    expect(session.accessToken, 'rotated-access-token');
    expect(session.refreshToken, 'rotated-refresh-token');
  });

  test('logout uses bearer access token and accepts 204 response', () async {
    final client = MockClient((request) async {
      expect(request.method, 'POST');
      expect(request.url.path, ApiAuthRemoteDataSource.logoutPath);
      expect(request.headers['authorization'], 'Bearer access-token');
      expect(request.body, isEmpty);
      return http.Response('', 204);
    });
    final apiClient = ApiClient(baseUrl: 'https://identity.test', httpClient: client);
    addTearDown(apiClient.close);
    final source = ApiAuthRemoteDataSource(apiClient: apiClient);

    await source.logout('access-token');
  });

  test('auth HTTP error is mapped through the stable backend error contract', () async {
    final client = MockClient((request) async {
      return http.Response(
        jsonEncode(<String, Object?>{
          'code': 'AUTH_INVALID_CREDENTIALS',
          'message': 'Invalid credentials.',
          'traceId': 'trace-123',
        }),
        401,
        headers: <String, String>{'content-type': 'application/json'},
      );
    });
    final apiClient = ApiClient(baseUrl: 'https://identity.test', httpClient: client);
    addTearDown(apiClient.close);
    final source = ApiAuthRemoteDataSource(apiClient: apiClient);

    await expectLater(
      source.login(credentials),
      throwsA(
        isA<AuthRemoteDataSourceException>()
            .having(
              (error) => error.error.code,
              'code',
              AuthErrorCode.invalidCredentials,
            )
            .having((error) => error.error.traceId, 'traceId', 'trace-123'),
      ),
    );
  });
}

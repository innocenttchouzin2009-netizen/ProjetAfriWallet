import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/identity_remote_data_source.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';

void main() {
  group('IdentityRemoteDataSource', () {
    test('reads current profile with bearer token', () async {
      late http.Request captured;
      final client = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          captured = request;
          return http.Response(
            jsonEncode(<String, Object?>{
              'userId': 'user-1',
              'identifier': 'user@example.com',
              'createdAtUtc': '2026-10-01T09:30:00Z',
            }),
            200,
          );
        }),
      );
      final source = IdentityRemoteDataSource(client);

      final profile = await source.loadCurrentProfile('test-access-token');

      expect(captured.url.path, '/api/v1/identity/current-profile');
      expect(captured.headers['Authorization'], 'Bearer test-access-token');
      expect(profile.identifier, 'user@example.com');
      client.close();
    });

    test('reads public AfWal ID without authorization', () async {
      late http.Request captured;
      final client = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((request) async {
          captured = request;
          return http.Response('{"afWalId":"public.user"}', 200);
        }),
      );
      final source = IdentityRemoteDataSource(client);

      final identity = await source.loadPublicAfWalId('public.user');

      expect(captured.url.path, '/api/v1/identity/afwal-id/public.user');
      expect(captured.headers.containsKey('Authorization'), isFalse);
      expect(identity.afWalId, 'public.user');
      client.close();
    });

    test('maps malformed identity payloads', () async {
      final client = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async => http.Response('{}', 200)),
      );
      final source = IdentityRemoteDataSource(client);

      await expectLater(
        source.loadPublicAfWalId('public.user'),
        throwsA(isA<ApiMalformedResponseException>()),
      );
      client.close();
    });

    test('preserves unauthorized current-profile responses', () async {
      final client = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient((_) async => http.Response('', 401)),
      );
      final source = IdentityRemoteDataSource(client);

      await expectLater(
        source.loadCurrentProfile('expired-test-token'),
        throwsA(isA<ApiUnauthorizedException>()),
      );
      client.close();
    });
  });
}

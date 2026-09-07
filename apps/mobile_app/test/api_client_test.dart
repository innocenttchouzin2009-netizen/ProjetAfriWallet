import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';

void main() {
  group('ApiClient', () {
    test('GET decodes JSON and forwards query parameters', () async {
      final mockClient = MockClient((request) async {
        expect(request.method, 'GET');
        expect(request.url.path, '/api/v1/health');
        expect(request.url.queryParameters, {'source': 'mobile'});
        expect(request.headers['Accept'], 'application/json');
        return http.Response('{"status":"ok"}', 200);
      });
      final client = ApiClient(
        baseUrl: 'https://api.example.test',
        httpClient: mockClient,
      );

      final result = await client.getJson(
        '/api/v1/health',
        queryParameters: const {'source': 'mobile'},
      );

      expect(result, {'status': 'ok'});
      client.close();
    });

    test('POST encodes JSON and merges headers', () async {
      final mockClient = MockClient((request) async {
        expect(request.method, 'POST');
        expect(request.headers['Content-Type'], 'application/json');
        expect(request.headers['Authorization'], 'Bearer token');
        expect(request.body, '{"name":"AfWal"}');
        return http.Response('{"created":true}', 201);
      });
      final client = ApiClient(
        baseUrl: 'https://api.example.test',
        httpClient: mockClient,
        defaultHeaders: const {'Authorization': 'Bearer token'},
      );

      final result = await client.postJson(
        '/resources',
        body: const {'name': 'AfWal'},
      );

      expect(result, {'created': true});
      client.close();
    });

    test('returns null for empty successful response', () async {
      final client = ApiClient(
        baseUrl: 'https://api.example.test',
        httpClient: MockClient((request) async => http.Response('', 204)),
      );

      expect(await client.getJson('/empty'), isNull);
      client.close();
    });

    test('maps 401 to ApiUnauthorizedException', () async {
      final client = ApiClient(
        baseUrl: 'https://api.example.test',
        httpClient: MockClient(
          (request) async => http.Response('{"error":"unauthorized"}', 401),
        ),
      );

      await expectLater(
        client.getJson('/private'),
        throwsA(isA<ApiUnauthorizedException>()),
      );
      client.close();
    });

    test('maps 409 to ApiConflictException', () async {
      final client = ApiClient(
        baseUrl: 'https://api.example.test',
        httpClient: MockClient((request) async => http.Response('conflict', 409)),
      );

      await expectLater(
        client.postJson('/resource'),
        throwsA(isA<ApiConflictException>()),
      );
      client.close();
    });

    test('maps 422 to ApiValidationException', () async {
      final client = ApiClient(
        baseUrl: 'https://api.example.test',
        httpClient: MockClient((request) async => http.Response('invalid', 422)),
      );

      await expectLater(
        client.postJson('/resource'),
        throwsA(isA<ApiValidationException>()),
      );
      client.close();
    });

    test('maps 5xx to ApiServerException', () async {
      final client = ApiClient(
        baseUrl: 'https://api.example.test',
        httpClient: MockClient((request) async => http.Response('down', 503)),
      );

      await expectLater(
        client.getJson('/health'),
        throwsA(
          isA<ApiServerException>().having(
            (error) => error.statusCode,
            'statusCode',
            503,
          ),
        ),
      );
      client.close();
    });

    test('maps malformed JSON to ApiMalformedResponseException', () async {
      final client = ApiClient(
        baseUrl: 'https://api.example.test',
        httpClient: MockClient((request) async => http.Response('not-json', 200)),
      );

      await expectLater(
        client.getJson('/broken'),
        throwsA(isA<ApiMalformedResponseException>()),
      );
      client.close();
    });

    test('maps timeout to ApiTimeoutException', () async {
      final client = ApiClient(
        baseUrl: 'https://api.example.test',
        timeout: const Duration(milliseconds: 1),
        httpClient: MockClient((request) async {
          await Future<void>.delayed(const Duration(milliseconds: 20));
          return http.Response('{"status":"ok"}', 200);
        }),
      );

      await expectLater(
        client.getJson('/slow'),
        throwsA(isA<ApiTimeoutException>()),
      );
      client.close();
    });

    test('maps ClientException to ApiNetworkException', () async {
      final client = ApiClient(
        baseUrl: 'https://api.example.test',
        httpClient: MockClient((request) async {
          throw http.ClientException('offline', request.url);
        }),
      );

      await expectLater(
        client.getJson('/offline'),
        throwsA(isA<ApiNetworkException>()),
      );
      client.close();
    });
  });
}

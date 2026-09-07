import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/health_remote_data_source.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';

void main() {
  group('HealthRemoteDataSource', () {
    test('maps GET /health status ok response', () async {
      late Uri requestedUri;
      final httpClient = MockClient((request) async {
        requestedUri = request.url;
        expect(request.method, 'GET');
        return http.Response('{"status":"ok"}', 200);
      });
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: httpClient,
      );
      final dataSource = HealthRemoteDataSource(apiClient);

      final result = await dataSource.check();

      expect(requestedUri.path, '/health');
      expect(result.status, 'ok');
      expect(result.isOk, isTrue);
      apiClient.close();
    });

    test('rejects a non-object health payload', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response('["ok"]', 200),
        ),
      );
      final dataSource = HealthRemoteDataSource(apiClient);

      await expectLater(
        dataSource.check(),
        throwsA(isA<ApiMalformedResponseException>()),
      );
      apiClient.close();
    });

    test('rejects a missing health status', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response('{}', 200),
        ),
      );
      final dataSource = HealthRemoteDataSource(apiClient);

      await expectLater(
        dataSource.check(),
        throwsA(isA<ApiMalformedResponseException>()),
      );
      apiClient.close();
    });

    test('propagates API server failures', () async {
      final apiClient = ApiClient(
        baseUrl: 'https://api.afwal.test',
        httpClient: MockClient(
          (_) async => http.Response('{"error":"down"}', 503),
        ),
      );
      final dataSource = HealthRemoteDataSource(apiClient);

      await expectLater(
        dataSource.check(),
        throwsA(
          isA<ApiServerException>().having(
            (error) => error.statusCode,
            'statusCode',
            503,
          ),
        ),
      );
      apiClient.close();
    });
  });
}

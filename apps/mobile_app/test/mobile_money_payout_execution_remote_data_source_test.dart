import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_execution_dto.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_execution_remote_data_source.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';

void main() {
  group('MobileMoneyPayoutExecutionRemoteDataSource', () {
    test('posts authenticated idempotent execution contract and parses response',
        () async {
      late http.Request capturedRequest;
      final dataSource = _dataSource(
        MockClient((request) async {
          capturedRequest = request;
          return http.Response(
            jsonEncode(_validResponse()),
            200,
            headers: <String, String>{'content-type': 'application/json'},
          );
        }),
      );

      final response = await dataSource.prepareExecution(
        'execution-access-token',
        'exec-42',
        _request(),
      );

      expect(capturedRequest.method, 'POST');
      expect(
        capturedRequest.url.path,
        '/api/v1/mobile-money/payouts/execution/prepare',
      );
      expect(
        capturedRequest.headers['Authorization'],
        'Bearer execution-access-token',
      );
      expect(capturedRequest.headers['Idempotency-Key'], 'exec-42');
      expect(
        jsonDecode(capturedRequest.body),
        <String, Object?>{
          'quoteId': '11111111-1111-4111-8111-111111111111',
          'fundingCorrelationId': '22222222-2222-4222-8222-222222222222',
        },
      );
      expect(
        response.bindingId,
        '33333333-3333-4333-8333-333333333333',
      );
      expect(response.status, 'Prepared');
      expect(response.isReplay, isFalse);
      expect(response.preparedAtUtc.isUtc, isTrue);
    });

    test('maps malformed execution response to ApiMalformedResponseException',
        () async {
      final dataSource = _dataSource(
        MockClient(
          (_) async => http.Response(
            jsonEncode(<String, Object?>{'bindingId': 'invalid'}),
            200,
          ),
        ),
      );

      await expectLater(
        dataSource.prepareExecution(
          'execution-access-token',
          'exec-42',
          _request(),
        ),
        throwsA(isA<ApiMalformedResponseException>()),
      );
    });

    test('preserves idempotency conflicts from execution endpoint', () async {
      final dataSource = _dataSource(
        MockClient(
          (_) async => http.Response(
            jsonEncode(<String, Object?>{
              'code': 'PAYOUT_EXECUTION_IDEMPOTENCY_CONFLICT',
              'message': 'The idempotency key conflicts with another request.',
            }),
            409,
            headers: <String, String>{'content-type': 'application/json'},
          ),
        ),
      );

      await expectLater(
        dataSource.prepareExecution(
          'execution-access-token',
          'exec-42',
          _request(),
        ),
        throwsA(isA<ApiConflictException>()),
      );
    });

    test('rejects an empty idempotency key before sending the request',
        () async {
      var called = false;
      final dataSource = _dataSource(
        MockClient((_) async {
          called = true;
          return http.Response('{}', 200);
        }),
      );

      await expectLater(
        dataSource.prepareExecution(
          'execution-access-token',
          '   ',
          _request(),
        ),
        throwsArgumentError,
      );
      expect(called, isFalse);
    });
  });
}

MobileMoneyPayoutExecutionRemoteDataSource _dataSource(http.Client client) {
  return MobileMoneyPayoutExecutionRemoteDataSource(
    ApiClient(
      baseUrl: 'https://api.afrikawallet.test',
      httpClient: client,
    ),
  );
}

MobileMoneyPayoutExecutionRequestDto _request() {
  return const MobileMoneyPayoutExecutionRequestDto(
    quoteId: '11111111-1111-4111-8111-111111111111',
    fundingCorrelationId: '22222222-2222-4222-8222-222222222222',
  );
}

Map<String, Object?> _validResponse() => <String, Object?>{
      'bindingId': '33333333-3333-4333-8333-333333333333',
      'quoteId': '11111111-1111-4111-8111-111111111111',
      'fundingCorrelationId': '22222222-2222-4222-8222-222222222222',
      'idempotencyKey': 'exec-42',
      'status': 'Prepared',
      'isReplay': false,
      'preparedAtUtc': '2026-10-12T06:00:00Z',
    };

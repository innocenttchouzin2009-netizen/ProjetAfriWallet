import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_funding_dto.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_funding_remote_data_source.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';

void main() {
  group('MobileMoneyPayoutFundingRemoteDataSource', () {
    test('posts the certified funding contract and parses the response',
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

      final response = await dataSource.planFunding(_request());

      expect(capturedRequest.method, 'POST');
      expect(
        capturedRequest.url.path,
        '/api/v1/mobile-money/payouts/funding/plan',
      );
      expect(
        jsonDecode(capturedRequest.body),
        <String, Object?>{
          'correlationId': '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4',
          'requiredAmountMinor': 10250,
          'currencyCode': 'EUR',
          'allocations': <Object?>[
            <String, Object?>{
              'sourceId': 'wallet-1',
              'sourceType': 0,
              'amountMinor': 2500,
              'currencyCode': 'EUR',
            },
            <String, Object?>{
              'sourceId': 'google-pay-1',
              'sourceType': 2,
              'amountMinor': 7750,
              'currencyCode': 'EUR',
            },
          ],
          'requestedAtUtc': '2026-10-10T10:30:00.000Z',
        },
      );
      expect(response.requiredAmountMinor, 10250);
      expect(response.allocations, hasLength(2));
      expect(response.plannedAtUtc.isUtc, isTrue);
    });

    test('maps a malformed funding response to ApiMalformedResponseException',
        () async {
      final dataSource = _dataSource(
        MockClient(
          (_) async => http.Response(
            jsonEncode(<String, Object?>{'correlationId': 'invalid'}),
            200,
          ),
        ),
      );

      await expectLater(
        dataSource.planFunding(_request()),
        throwsA(isA<ApiMalformedResponseException>()),
      );
    });

    test('preserves API validation errors from the funding endpoint', () async {
      final dataSource = _dataSource(
        MockClient(
          (_) async => http.Response(
            jsonEncode(<String, Object?>{
              'code': 'PAYOUT_FUNDING_REJECTED',
              'message': 'The payout funding plan could not be accepted.',
            }),
            422,
            headers: <String, String>{'content-type': 'application/json'},
          ),
        ),
      );

      await expectLater(
        dataSource.planFunding(_request()),
        throwsA(isA<ApiValidationException>()),
      );
    });
  });
}

MobileMoneyPayoutFundingRemoteDataSource _dataSource(http.Client client) {
  return MobileMoneyPayoutFundingRemoteDataSource(
    ApiClient(
      baseUrl: 'https://api.afrikawallet.test',
      httpClient: client,
    ),
  );
}

MobileMoneyPayoutFundingRequestDto _request() {
  return MobileMoneyPayoutFundingRequestDto(
    correlationId: '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4',
    requiredAmountMinor: 10250,
    currencyCode: 'EUR',
    allocations: const <MobileMoneyPayoutFundingAllocationDto>[
      MobileMoneyPayoutFundingAllocationDto(
        sourceId: 'wallet-1',
        sourceType: 0,
        amountMinor: 2500,
        currencyCode: 'EUR',
      ),
      MobileMoneyPayoutFundingAllocationDto(
        sourceId: 'google-pay-1',
        sourceType: 2,
        amountMinor: 7750,
        currencyCode: 'EUR',
      ),
    ],
    requestedAtUtc: DateTime.utc(2026, 10, 10, 10, 30),
  );
}

Map<String, dynamic> _validResponse() => <String, dynamic>{
      'correlationId': '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4',
      'requiredAmountMinor': 10250,
      'currencyCode': 'EUR',
      'allocations': <Object?>[
        <String, dynamic>{
          'sourceId': 'wallet-1',
          'sourceType': 0,
          'amountMinor': 2500,
          'currencyCode': 'EUR',
        },
        <String, dynamic>{
          'sourceId': 'google-pay-1',
          'sourceType': 2,
          'amountMinor': 7750,
          'currencyCode': 'EUR',
        },
      ],
      'plannedAtUtc': '2026-10-10T10:30:01Z',
    };

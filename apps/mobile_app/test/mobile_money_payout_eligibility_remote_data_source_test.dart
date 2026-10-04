import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_eligibility_dto.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_eligibility_remote_data_source.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';

void main() {
  const requestDto = MobileMoneyPayoutEligibilityRequestDto(
    sourceCountryCode: 'DE',
    sourceCurrency: 'EUR',
    destinationCountryCode: 'CM',
    destinationCurrency: 'XAF',
    operatorCode: 'MTN_CM',
  );

  test('posts the exact payout eligibility contract and maps success', () async {
    late http.Request capturedRequest;

    final apiClient = ApiClient(
      baseUrl: 'https://api.afrikawallet.test',
      httpClient: MockClient((request) async {
        capturedRequest = request;
        return http.Response(
          jsonEncode(<String, Object?>{
            'isEligible': true,
            'failureCode': null,
          }),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        );
      }),
    );
    addTearDown(apiClient.close);

    final dataSource = MobileMoneyPayoutEligibilityRemoteDataSource(apiClient);

    final result = await dataSource.checkEligibility(requestDto);

    expect(capturedRequest.method, 'POST');
    expect(
      capturedRequest.url.path,
      '/api/v1/mobile-money/payouts/eligibility',
    );
    expect(
      jsonDecode(capturedRequest.body),
      <String, Object?>{
        'sourceCountryCode': 'DE',
        'sourceCurrency': 'EUR',
        'destinationCountryCode': 'CM',
        'destinationCurrency': 'XAF',
        'operatorCode': 'MTN_CM',
      },
    );
    expect(result.isEligible, isTrue);
    expect(result.failureCode, isNull);
  });

  test('preserves a backend payout eligibility failure code', () async {
    final apiClient = ApiClient(
      baseUrl: 'https://api.afrikawallet.test',
      httpClient: MockClient(
        (_) async => http.Response(
          jsonEncode(<String, Object?>{
            'isEligible': false,
            'failureCode': 'PAYOUT_OPERATOR_NOT_ACTIVATED',
          }),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        ),
      ),
    );
    addTearDown(apiClient.close);

    final dataSource = MobileMoneyPayoutEligibilityRemoteDataSource(apiClient);

    final result = await dataSource.checkEligibility(requestDto);

    expect(result.isEligible, isFalse);
    expect(result.failureCode, 'PAYOUT_OPERATOR_NOT_ACTIVATED');
  });

  test('maps an invalid eligibility payload to an API response error', () async {
    final apiClient = ApiClient(
      baseUrl: 'https://api.afrikawallet.test',
      httpClient: MockClient(
        (_) async => http.Response(
          jsonEncode(<String, Object?>{
            'isEligible': 'true',
            'failureCode': null,
          }),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        ),
      ),
    );
    addTearDown(apiClient.close);

    final dataSource = MobileMoneyPayoutEligibilityRemoteDataSource(apiClient);

    expect(
      () => dataSource.checkEligibility(requestDto),
      throwsA(isA<ApiMalformedResponseException>()),
    );
  });
}

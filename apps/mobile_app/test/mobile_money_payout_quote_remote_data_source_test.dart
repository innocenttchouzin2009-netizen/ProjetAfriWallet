import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_quote_dto.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_quote_remote_data_source.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';

void main() {
  const requestDto = MobileMoneyPayoutQuoteRequestDto(
    sourceCountryCode: 'DE',
    sourceCurrency: 'EUR',
    destinationCountryCode: 'CM',
    destinationCurrency: 'XAF',
    operatorCode: 'MTN-CM',
    sourceAmountMinor: 10000,
  );

  test('posts the exact payout quote contract and maps success', () async {
    late http.Request capturedRequest;

    final apiClient = ApiClient(
      baseUrl: 'https://api.afrikawallet.test',
      httpClient: MockClient((request) async {
        capturedRequest = request;
        return http.Response(
          jsonEncode(_validPayload()),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        );
      }),
    );
    addTearDown(apiClient.close);

    final dataSource = MobileMoneyPayoutQuoteRemoteDataSource(apiClient);

    final result = await dataSource.createQuote(requestDto);

    expect(capturedRequest.method, 'POST');
    expect(capturedRequest.url.path, '/api/v1/mobile-money/payouts/quote');
    expect(
      jsonDecode(capturedRequest.body),
      <String, Object?>{
        'sourceCountryCode': 'DE',
        'sourceCurrency': 'EUR',
        'destinationCountryCode': 'CM',
        'destinationCurrency': 'XAF',
        'operatorCode': 'MTN-CM',
        'sourceAmountMinor': 10000,
      },
    );
    expect(result.quoteId, 'dd5d65b9-83ba-463a-87f8-d4b8513c8595');
    expect(result.sourceCurrency, 'EUR');
    expect(result.sourceAmountMinor, 10000);
    expect(result.fees, hasLength(1));
    expect(result.totalFeeMinor, 250);
    expect(result.totalSourceDebitMinor, 10250);
    expect(result.destinationCurrency, 'XAF');
    expect(result.destinationAmountMinor, 6559570);
    expect(result.fxRate, 655.957);
    expect(result.createdAtUtc.isUtc, isTrue);
    expect(result.expiresAtUtc.isUtc, isTrue);
  });

  test('maps an invalid payout quote payload to an API response error', () async {
    final apiClient = ApiClient(
      baseUrl: 'https://api.afrikawallet.test',
      httpClient: MockClient(
        (_) async => http.Response(
          jsonEncode(_validPayload()..['sourceAmountMinor'] = 0),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        ),
      ),
    );
    addTearDown(apiClient.close);

    final dataSource = MobileMoneyPayoutQuoteRemoteDataSource(apiClient);

    expect(
      () => dataSource.createQuote(requestDto),
      throwsA(isA<ApiMalformedResponseException>()),
    );
  });

  test('maps malformed nested quote fees to an API response error', () async {
    final apiClient = ApiClient(
      baseUrl: 'https://api.afrikawallet.test',
      httpClient: MockClient(
        (_) async => http.Response(
          jsonEncode(_validPayload()..['fees'] = <Object?>['invalid']),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        ),
      ),
    );
    addTearDown(apiClient.close);

    final dataSource = MobileMoneyPayoutQuoteRemoteDataSource(apiClient);

    expect(
      () => dataSource.createQuote(requestDto),
      throwsA(isA<ApiMalformedResponseException>()),
    );
  });

  test('preserves API validation errors from the payout quote endpoint', () async {
    final apiClient = ApiClient(
      baseUrl: 'https://api.afrikawallet.test',
      httpClient: MockClient(
        (_) async => http.Response(
          jsonEncode(<String, Object?>{
            'code': 'PAYOUT_CORRIDOR_NOT_ELIGIBLE',
            'message': 'The requested payout corridor is not eligible.',
          }),
          422,
          headers: <String, String>{'content-type': 'application/json'},
        ),
      ),
    );
    addTearDown(apiClient.close);

    final dataSource = MobileMoneyPayoutQuoteRemoteDataSource(apiClient);

    expect(
      () => dataSource.createQuote(requestDto),
      throwsA(isA<ApiValidationException>()),
    );
  });
}

Map<String, dynamic> _validPayload() => <String, dynamic>{
  'quoteId': 'dd5d65b9-83ba-463a-87f8-d4b8513c8595',
  'sourceCurrency': 'EUR',
  'sourceAmountMinor': 10000,
  'fees': <Object?>[
    <String, dynamic>{
      'code': 'SERVICE_FEE',
      'amountMinor': 250,
      'currency': 'EUR',
    },
  ],
  'totalFeeMinor': 250,
  'totalSourceDebitMinor': 10250,
  'destinationCurrency': 'XAF',
  'destinationAmountMinor': 6559570,
  'fxRate': 655.957,
  'createdAtUtc': '2026-10-07T00:00:00+00:00',
  'expiresAtUtc': '2026-10-07T00:10:00+00:00',
};

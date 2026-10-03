import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_remote_data_source.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/network/api_exception.dart';

void main() {
  group('MobileMoneyPayoutRemoteDataSource', () {
    test('posts the exact backend eligibility contract and parses eligible',
        () async {
      final client = MockClient((request) async {
        expect(request.method, 'POST');
        expect(request.url.path, '/api/v1/mobile-money/payouts/eligibility');
        expect(
          jsonDecode(request.body),
          <String, Object?>{
            'sourceCountryCode': 'DE',
            'sourceCurrency': 'EUR',
            'destinationCountryCode': 'CM',
            'destinationCurrency': 'XAF',
            'operatorCode': 'mtn-cm',
          },
        );

        return http.Response(
          jsonEncode(<String, Object?>{
            'isEligible': true,
            'failureCode': null,
          }),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        );
      });

      final dataSource = MobileMoneyPayoutRemoteDataSource(
        ApiClient(baseUrl: 'https://api.afrikawallet.test', httpClient: client),
      );

      final result = await dataSource.checkEligibility(
        sourceCountryCode: 'DE',
        payout: _payout(),
      );

      expect(result.isEligible, isTrue);
      expect(result.failureCode, isNull);
    });

    test('parses an ineligible corridor failure code', () async {
      final client = MockClient((request) async {
        return http.Response(
          jsonEncode(<String, Object?>{
            'isEligible': false,
            'failureCode': 'PAYOUT_CORRIDOR_DISABLED',
          }),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        );
      });

      final dataSource = MobileMoneyPayoutRemoteDataSource(
        ApiClient(baseUrl: 'https://api.afrikawallet.test', httpClient: client),
      );

      final result = await dataSource.checkEligibility(
        sourceCountryCode: 'DE',
        payout: _payout(),
      );

      expect(result.isEligible, isFalse);
      expect(result.failureCode, 'PAYOUT_CORRIDOR_DISABLED');
    });

    test('rejects malformed eligibility payloads', () async {
      final client = MockClient((request) async {
        return http.Response(
          jsonEncode(<String, Object?>{
            'isEligible': 'yes',
            'failureCode': null,
          }),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        );
      });

      final dataSource = MobileMoneyPayoutRemoteDataSource(
        ApiClient(baseUrl: 'https://api.afrikawallet.test', httpClient: client),
      );

      expect(
        () => dataSource.checkEligibility(
          sourceCountryCode: 'DE',
          payout: _payout(),
        ),
        throwsA(isA<ApiMalformedResponseException>()),
      );
    });

    test('propagates API validation failures from ApiClient', () async {
      final client = MockClient((request) async {
        return http.Response(
          jsonEncode(<String, Object?>{
            'code': 'PAYOUT_INVALID_DESTINATION',
            'message': 'The payout destination or corridor is invalid.',
          }),
          422,
          headers: <String, String>{'content-type': 'application/json'},
        );
      });

      final dataSource = MobileMoneyPayoutRemoteDataSource(
        ApiClient(baseUrl: 'https://api.afrikawallet.test', httpClient: client),
      );

      expect(
        () => dataSource.checkEligibility(
          sourceCountryCode: 'DE',
          payout: _payout(),
        ),
        throwsA(isA<ApiValidationException>()),
      );
    });
  });
}

MobileMoneyPayoutRequest _payout() {
  return MobileMoneyPayoutRequest(
    beneficiary: const MobileMoneyBeneficiary(
      displayName: 'Ama Mensah',
      phoneNumberE164: '+237612345678',
      countryCode: 'CM',
      operatorCode: 'mtn-cm',
      currencyCode: 'XAF',
    ),
    sendAmountMinor: 10000,
    sendCurrencyCode: 'EUR',
    payoutAmountMinor: 655000,
    payoutCurrencyCode: 'XAF',
    fundingAllocations: const <FundingAllocation>[
      FundingAllocation(
        sourceId: 'sepa-method-1',
        sourceType: FundingSourceType.sepa,
        amountMinor: 10000,
        currencyCode: 'EUR',
      ),
    ],
    idempotencyKey: 'payout-request-1',
  );
}

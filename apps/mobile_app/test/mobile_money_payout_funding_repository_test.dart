import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_funding_remote_data_source.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_funding_intent.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/services/mobile_money_payout_funding_repository.dart';

void main() {
  group('RemoteMobileMoneyPayoutFundingRepository', () {
    test('maps a certified funding intent to a domain funding plan', () async {
      late http.Request capturedRequest;
      final repository = _repository(
        MockClient((request) async {
          capturedRequest = request;
          return http.Response(
            jsonEncode(_validResponse()),
            200,
            headers: <String, String>{'content-type': 'application/json'},
          );
        }),
      );

      final plan = await repository.planFunding(
        correlationId: '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4',
        intent: _intent(),
        requestedAtUtc: DateTime.utc(2026, 10, 10, 10, 30),
      );

      expect(
        capturedRequest.url.path,
        '/api/v1/mobile-money/payouts/funding/plan',
      );
      expect(plan.correlationId, '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4');
      expect(plan.requiredAmountMinor, 10250);
      expect(plan.currencyCode, 'EUR');
      expect(plan.allocations, hasLength(2));
      expect(plan.allocations.first.sourceType, FundingSourceType.wallet);
      expect(plan.allocations.last.sourceType, FundingSourceType.googlePay);
      expect(plan.plannedAtUtc, DateTime.utc(2026, 10, 10, 10, 30, 1));
    });

    test('rejects an invalid funding intent before the network call', () async {
      var called = false;
      final repository = _repository(
        MockClient((_) async {
          called = true;
          return http.Response('{}', 200);
        }),
      );

      await expectLater(
        repository.planFunding(
          correlationId: '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4',
          intent: MobileMoneyPayoutFundingIntent(
            quoteId: 'quote-1',
            sourceCurrencyCode: 'EUR',
            totalSourceDebitMinor: 10250,
            fundingAllocations: const <FundingAllocation>[
              FundingAllocation(
                sourceId: 'wallet-1',
                sourceType: FundingSourceType.wallet,
                amountMinor: 2500,
                currencyCode: 'EUR',
              ),
            ],
          ),
          requestedAtUtc: DateTime.utc(2026, 10, 10, 10, 30),
        ),
        throwsArgumentError,
      );
      expect(called, isFalse);
    });

    test('rejects non-UTC request time before the network call', () async {
      var called = false;
      final repository = _repository(
        MockClient((_) async {
          called = true;
          return http.Response('{}', 200);
        }),
      );

      await expectLater(
        repository.planFunding(
          correlationId: '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4',
          intent: _intent(),
          requestedAtUtc: DateTime(2026, 10, 10, 10, 30),
        ),
        throwsArgumentError,
      );
      expect(called, isFalse);
    });
  });
}

RemoteMobileMoneyPayoutFundingRepository _repository(http.Client client) {
  return RemoteMobileMoneyPayoutFundingRepository(
    MobileMoneyPayoutFundingRemoteDataSource(
      ApiClient(
        baseUrl: 'https://api.afrikawallet.test',
        httpClient: client,
      ),
    ),
  );
}

MobileMoneyPayoutFundingIntent _intent() {
  return MobileMoneyPayoutFundingIntent(
    quoteId: 'quote-1',
    sourceCurrencyCode: 'EUR',
    totalSourceDebitMinor: 10250,
    fundingAllocations: const <FundingAllocation>[
      FundingAllocation(
        sourceId: 'wallet-1',
        sourceType: FundingSourceType.wallet,
        amountMinor: 2500,
        currencyCode: 'EUR',
      ),
      FundingAllocation(
        sourceId: 'google-pay-1',
        sourceType: FundingSourceType.googlePay,
        amountMinor: 7750,
        currencyCode: 'EUR',
      ),
    ],
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

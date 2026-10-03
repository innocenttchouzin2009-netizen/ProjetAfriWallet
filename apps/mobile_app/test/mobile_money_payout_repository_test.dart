import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_remote_data_source.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/services/mobile_money_payout_repository.dart';

void main() {
  group('RemoteMobileMoneyPayoutRepository', () {
    test('validates and maps an eligible payout result', () async {
      final client = MockClient((request) async {
        return http.Response(
          jsonEncode(<String, Object?>{
            'isEligible': true,
            'failureCode': null,
          }),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        );
      });
      final repository = _repository(client);

      final result = await repository.checkEligibility(
        sourceCountryCode: 'DE',
        payout: _externalPayout(),
      );

      expect(result.isEligible, isTrue);
      expect(result.failureCode, isNull);
    });

    test('maps an ineligible corridor failure code', () async {
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
      final repository = _repository(client);

      final result = await repository.checkEligibility(
        sourceCountryCode: 'DE',
        payout: _externalPayout(),
      );

      expect(result.isEligible, isFalse);
      expect(result.failureCode, 'PAYOUT_CORRIDOR_DISABLED');
    });

    test('rejects an invalid source country before the network call', () async {
      var called = false;
      final client = MockClient((request) async {
        called = true;
        return http.Response('{}', 200);
      });
      final repository = _repository(client);

      expect(
        () => repository.checkEligibility(
          sourceCountryCode: 'de',
          payout: _externalPayout(),
        ),
        throwsArgumentError,
      );
      expect(called, isFalse);
    });

    test('rejects an invalid payout before the network call', () async {
      var called = false;
      final client = MockClient((request) async {
        called = true;
        return http.Response('{}', 200);
      });
      final repository = _repository(client);

      expect(
        () => repository.checkEligibility(
          sourceCountryCode: 'DE',
          payout: _externalPayout(sendAmountMinor: 0),
        ),
        throwsArgumentError,
      );
      expect(called, isFalse);
    });

    test('validates wallet availability before the network call', () async {
      var called = false;
      final client = MockClient((request) async {
        called = true;
        return http.Response('{}', 200);
      });
      final repository = _repository(client);

      expect(
        () => repository.checkEligibility(
          sourceCountryCode: 'DE',
          payout: _walletPayout(amountMinor: 3501),
          fundingSources: <FundingSource>[
            _walletSource(availableMinor: 3500),
          ],
        ),
        throwsArgumentError,
      );
      expect(called, isFalse);
    });

    test('accepts wallet funding when availability covers allocation', () async {
      final client = MockClient((request) async {
        return http.Response(
          jsonEncode(<String, Object?>{
            'isEligible': true,
            'failureCode': null,
          }),
          200,
          headers: <String, String>{'content-type': 'application/json'},
        );
      });
      final repository = _repository(client);

      final result = await repository.checkEligibility(
        sourceCountryCode: 'DE',
        payout: _walletPayout(amountMinor: 3500),
        fundingSources: <FundingSource>[
          _walletSource(availableMinor: 3500),
        ],
      );

      expect(result.isEligible, isTrue);
    });
  });
}

RemoteMobileMoneyPayoutRepository _repository(http.Client client) {
  return RemoteMobileMoneyPayoutRepository(
    MobileMoneyPayoutRemoteDataSource(
      ApiClient(
        baseUrl: 'https://api.afrikawallet.test',
        httpClient: client,
      ),
    ),
  );
}

MobileMoneyPayoutRequest _externalPayout({int sendAmountMinor = 10000}) {
  return MobileMoneyPayoutRequest(
    beneficiary: const MobileMoneyBeneficiary(
      displayName: 'Ama Mensah',
      phoneNumberE164: '+237612345678',
      countryCode: 'CM',
      operatorCode: 'mtn-cm',
      currencyCode: 'XAF',
    ),
    sendAmountMinor: sendAmountMinor,
    sendCurrencyCode: 'EUR',
    payoutAmountMinor: 655000,
    payoutCurrencyCode: 'XAF',
    fundingAllocations: <FundingAllocation>[
      FundingAllocation(
        sourceId: 'sepa-method-1',
        sourceType: FundingSourceType.sepa,
        amountMinor: sendAmountMinor,
        currencyCode: 'EUR',
      ),
    ],
    idempotencyKey: 'payout-request-1',
  );
}

MobileMoneyPayoutRequest _walletPayout({required int amountMinor}) {
  return MobileMoneyPayoutRequest(
    beneficiary: const MobileMoneyBeneficiary(
      displayName: 'Ama Mensah',
      phoneNumberE164: '+237612345678',
      countryCode: 'CM',
      operatorCode: 'mtn-cm',
      currencyCode: 'XAF',
    ),
    sendAmountMinor: amountMinor,
    sendCurrencyCode: 'EUR',
    payoutAmountMinor: 230000,
    payoutCurrencyCode: 'XAF',
    fundingAllocations: <FundingAllocation>[
      FundingAllocation(
        sourceId: 'wallet-1',
        sourceType: FundingSourceType.wallet,
        amountMinor: amountMinor,
        currencyCode: 'EUR',
      ),
    ],
    idempotencyKey: 'wallet-payout-request-1',
  );
}

FundingSource _walletSource({required int availableMinor}) {
  return FundingSource(
    id: 'wallet-1',
    type: FundingSourceType.wallet,
    displayLabel: 'AfrikaWallet',
    currencyCode: 'EUR',
    isAvailable: true,
    availableMinor: availableMinor,
  );
}

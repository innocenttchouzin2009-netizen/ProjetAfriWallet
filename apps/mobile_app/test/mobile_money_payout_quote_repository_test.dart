import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:mobile_app/data/remote/mobile_money_payout_quote_remote_data_source.dart';
import 'package:mobile_app/network/api_client.dart';
import 'package:mobile_app/models/mobile_money_payout_quote_intent.dart';
import 'package:mobile_app/network/api_exception.dart';
import 'package:mobile_app/services/mobile_money_payout_quote_repository.dart';

void main() {
  group('RemoteMobileMoneyPayoutQuoteRepository', () {
    test('maps the quote request contract and response to the domain model',
        () async {
      late http.Request capturedRequest;
      final repository = _repository(
        MockClient((request) async {
          capturedRequest = request;
          return http.Response(
            jsonEncode(_validPayload()),
            200,
            headers: <String, String>{'content-type': 'application/json'},
          );
        }),
      );

      final quote = await repository.createQuote(
        sourceCountryCode: 'DE',
        sourceCurrencyCode: 'EUR',
        destinationCountryCode: 'CM',
        destinationCurrencyCode: 'XAF',
        operatorCode: 'MTN-CM',
        sourceAmountMinor: 10000,
      );

      expect(capturedRequest.method, 'POST');
      expect(
        capturedRequest.url.path,
        '/api/v1/mobile-money/payouts/quote',
      );
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

      expect(quote.quoteId, 'dd5d65b9-83ba-463a-87f8-d4b8513c8595');
      expect(quote.sourceCurrencyCode, 'EUR');
      expect(quote.sourceAmountMinor, 10000);
      expect(quote.fees, hasLength(1));
      expect(quote.fees.single.code, 'SERVICE_FEE');
      expect(quote.fees.single.amountMinor, 250);
      expect(quote.fees.single.currencyCode, 'EUR');
      expect(quote.totalFeeMinor, 250);
      expect(quote.totalSourceDebitMinor, 10250);
      expect(quote.destinationCurrencyCode, 'XAF');
      expect(quote.destinationAmountMinor, 6559570);
      expect(quote.fxRate, 655.957);
      expect(quote.createdAtUtc.isUtc, isTrue);
      expect(quote.expiresAtUtc.isUtc, isTrue);
    });

    test('creates a quote from the certified payout quote intent', () async {
      late http.Request capturedRequest;
      final repository = _repository(
        MockClient((request) async {
          capturedRequest = request;
          return http.Response(
            jsonEncode(_validPayload()),
            200,
            headers: <String, String>{'content-type': 'application/json'},
          );
        }),
      );

      final quote = await repository.createQuoteFromIntent(
        const MobileMoneyPayoutQuoteIntent(
          sourceCountryCode: 'DE',
          sourceCurrencyCode: 'EUR',
          destinationCurrencyCode: 'XAF',
          sourceAmountMinor: 10000,
          beneficiary: BeneficiaryQuoteDraft(
            normalizedPhoneNumber: '+237650000000',
            countryCode: 'CM',
            operatorCode: 'MTN_CM',
            accountHolderName: 'Jane Doe',
          ),
        ),
      );

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
      expect(quote.quoteId, 'dd5d65b9-83ba-463a-87f8-d4b8513c8595');
    });

    test('rejects an invalid payout quote intent before the network call',
        () async {
      var called = false;
      final repository = _repository(
        MockClient((_) async {
          called = true;
          return http.Response('{}', 200);
        }),
      );

      await expectLater(
        repository.createQuoteFromIntent(
          const MobileMoneyPayoutQuoteIntent(
            sourceCountryCode: 'DE',
            sourceCurrencyCode: 'EUR',
            destinationCurrencyCode: 'XAF',
            sourceAmountMinor: 0,
            beneficiary: BeneficiaryQuoteDraft(
              normalizedPhoneNumber: '+237650000000',
              countryCode: 'CM',
              operatorCode: 'MTN_CM',
              accountHolderName: 'Jane Doe',
            ),
          ),
        ),
        throwsArgumentError,
      );
      expect(called, isFalse);
    });

    test('rejects invalid country and currency codes before the network call',
        () async {
      var called = false;
      final repository = _repository(
        MockClient((_) async {
          called = true;
          return http.Response('{}', 200);
        }),
      );

      await expectLater(
        repository.createQuote(
          sourceCountryCode: 'de',
          sourceCurrencyCode: 'EUR',
          destinationCountryCode: 'CM',
          destinationCurrencyCode: 'XAF',
          operatorCode: 'MTN-CM',
          sourceAmountMinor: 10000,
        ),
        throwsArgumentError,
      );
      expect(called, isFalse);

      await expectLater(
        repository.createQuote(
          sourceCountryCode: 'DE',
          sourceCurrencyCode: 'eur',
          destinationCountryCode: 'CM',
          destinationCurrencyCode: 'XAF',
          operatorCode: 'MTN-CM',
          sourceAmountMinor: 10000,
        ),
        throwsArgumentError,
      );
      expect(called, isFalse);
    });

    test('rejects an empty operator and non-positive amount before network',
        () async {
      var called = false;
      final repository = _repository(
        MockClient((_) async {
          called = true;
          return http.Response('{}', 200);
        }),
      );

      await expectLater(
        repository.createQuote(
          sourceCountryCode: 'DE',
          sourceCurrencyCode: 'EUR',
          destinationCountryCode: 'CM',
          destinationCurrencyCode: 'XAF',
          operatorCode: '   ',
          sourceAmountMinor: 10000,
        ),
        throwsArgumentError,
      );
      expect(called, isFalse);

      await expectLater(
        repository.createQuote(
          sourceCountryCode: 'DE',
          sourceCurrencyCode: 'EUR',
          destinationCountryCode: 'CM',
          destinationCurrencyCode: 'XAF',
          operatorCode: 'MTN-CM',
          sourceAmountMinor: 0,
        ),
        throwsArgumentError,
      );
      expect(called, isFalse);
    });

    test('preserves API validation failures from the quote endpoint', () async {
      final repository = _repository(
        MockClient(
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

      await expectLater(
        repository.createQuote(
          sourceCountryCode: 'DE',
          sourceCurrencyCode: 'EUR',
          destinationCountryCode: 'CM',
          destinationCurrencyCode: 'XAF',
          operatorCode: 'MTN-CM',
          sourceAmountMinor: 10000,
        ),
        throwsA(isA<ApiValidationException>()),
      );
    });
  });
}

RemoteMobileMoneyPayoutQuoteRepository _repository(http.Client client) {
  return RemoteMobileMoneyPayoutQuoteRepository(
    MobileMoneyPayoutQuoteRemoteDataSource(
      ApiClient(
        baseUrl: 'https://api.afrikawallet.test',
        httpClient: client,
      ),
    ),
  );
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

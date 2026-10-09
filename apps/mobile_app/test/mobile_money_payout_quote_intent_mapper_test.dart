import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_beneficiary_lookup.dart';
import 'package:mobile_app/models/mobile_money_beneficiary_operator.dart';
import 'package:mobile_app/models/mobile_money_payout_quote_intent.dart';
import 'package:mobile_app/services/mobile_money_beneficiary_lookup_repository.dart';
import 'package:mobile_app/services/mobile_money_payout_quote_intent_mapper.dart';

void main() {
  group('MobileMoneyPayoutQuoteIntentMapper', () {
    test('creates a quote-ready beneficiary draft from a resolved lookup', () {
      final draft =
          MobileMoneyPayoutQuoteIntentMapper.beneficiaryFromSearchResult(
        _resolvedSearchResult(),
      );

      expect(draft.normalizedPhoneNumber, '+237670123456');
      expect(draft.countryCode, 'CM');
      expect(draft.operatorCode, 'MTN_CM');
      expect(draft.accountHolderName, 'Ada N.');
    });

    test('maps the quote intent to the existing backend quote contract', () {
      final draft =
          MobileMoneyPayoutQuoteIntentMapper.beneficiaryFromSearchResult(
        _resolvedSearchResult(),
      );
      final intent = MobileMoneyPayoutQuoteIntent(
        sourceCountryCode: 'DE',
        sourceCurrencyCode: 'EUR',
        destinationCurrencyCode: 'XAF',
        sourceAmountMinor: 10000,
        beneficiary: draft,
      );

      final request =
          MobileMoneyPayoutQuoteIntentMapper.toRequestDto(intent);

      expect(request.sourceCountryCode, 'DE');
      expect(request.sourceCurrency, 'EUR');
      expect(request.destinationCountryCode, 'CM');
      expect(request.destinationCurrency, 'XAF');
      expect(request.operatorCode, 'MTN-CM');
      expect(request.sourceAmountMinor, 10000);
    });

    test('rejects an unresolved beneficiary before quote creation', () {
      const unresolved = MobileMoneyBeneficiarySearchResult(
        lookup: MobileMoneyBeneficiaryLookup(
          normalizedPhoneNumber: '+237660123456',
          countryCode: 'CM',
          operator: null,
          operatorResolved: false,
          accountHolderName: null,
          beneficiaryResolved: false,
        ),
        operator: null,
      );

      expect(
        () => MobileMoneyPayoutQuoteIntentMapper.beneficiaryFromSearchResult(
          unresolved,
        ),
        throwsStateError,
      );
    });

    test('rejects an invalid quote intent before creating a request DTO', () {
      const intent = MobileMoneyPayoutQuoteIntent(
        sourceCountryCode: 'DE',
        sourceCurrencyCode: 'EUR',
        destinationCurrencyCode: 'XAF',
        sourceAmountMinor: 0,
        beneficiary: BeneficiaryQuoteDraft(
          normalizedPhoneNumber: '+237670123456',
          countryCode: 'CM',
          operatorCode: 'MTN_CM',
          accountHolderName: 'Ada N.',
        ),
      );

      expect(
        () => MobileMoneyPayoutQuoteIntentMapper.toRequestDto(intent),
        throwsArgumentError,
      );
    });
  });
}

MobileMoneyBeneficiarySearchResult _resolvedSearchResult() {
  return const MobileMoneyBeneficiarySearchResult(
    lookup: MobileMoneyBeneficiaryLookup(
      normalizedPhoneNumber: '+237670123456',
      countryCode: 'CM',
      operator: 'MTN',
      operatorResolved: true,
      accountHolderName: 'Ada N.',
      beneficiaryResolved: true,
    ),
    operator: MobileMoneyBeneficiaryOperator.mtnCameroon,
  );
}

import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';

void main() {
  group('MobileMoneyBeneficiary', () {
    test('allows an ad-hoc beneficiary without an id', () {
      final beneficiary = _beneficiary();

      expect(beneficiary.beneficiaryId, isNull);
      expect(beneficiary.phoneNumberE164, '+237612345678');
      expect(beneficiary.validate, returnsNormally);
    });

    test('accepts a saved beneficiary', () {
      expect(
        _beneficiary(beneficiaryId: 'beneficiary-1').validate,
        returnsNormally,
      );
    });

    test('rejects empty or malformed canonical fields', () {
      expect(_beneficiary(phoneNumberE164: '').validate, throwsArgumentError);
      expect(
        _beneficiary(phoneNumberE164: '237612345678').validate,
        throwsArgumentError,
      );
      expect(_beneficiary(countryCode: 'cm').validate, throwsArgumentError);
      expect(_beneficiary(currencyCode: 'xaf').validate, throwsArgumentError);
      expect(_beneficiary(operatorCode: ' ').validate, throwsArgumentError);
      expect(_beneficiary(displayName: ' ').validate, throwsArgumentError);
    });

    test('rejects an empty provided beneficiary id', () {
      expect(_beneficiary(beneficiaryId: ' ').validate, throwsArgumentError);
    });
  });

  group('FundingSource', () {
    test('wallet carries its available balance', () {
      final source = _walletSource(availableMinor: 3500);

      source.validate();
      expect(source.availableMinor, 3500);
    });

    test('external sources do not carry balances or sensitive details', () {
      for (final type in FundingSourceType.values.skip(1)) {
        final source = FundingSource(
          id: 'source-${type.name}',
          type: type,
          displayLabel: type.name,
          currencyCode: 'EUR',
          isAvailable: true,
        );

        expect(source.availableMinor, isNull);
        expect(source.validate, returnsNormally);
      }
    });

    test('requires non-negative availability for a wallet only', () {
      expect(_walletSource(availableMinor: null).validate, throwsArgumentError);
      expect(_walletSource(availableMinor: -1).validate, throwsArgumentError);
      expect(
        _applePaySource(availableMinor: 100).validate,
        throwsArgumentError,
      );
    });
  });

  group('FundingAllocation', () {
    test('requires a positive amount and a source id', () {
      expect(_allocation(amountMinor: 0).validate, throwsArgumentError);
      expect(_allocation(amountMinor: -1).validate, throwsArgumentError);
      expect(_allocation(sourceId: ' ').validate, throwsArgumentError);
      expect(_allocation(currencyCode: 'eur').validate, throwsArgumentError);
    });
  });

  group('MobileMoneyPayoutRequest', () {
    test('supports split funding and different payout currency', () {
      final request = _request(
        fundingAllocations: [
          _allocation(
            sourceId: 'wallet-1',
            sourceType: FundingSourceType.wallet,
            amountMinor: 3500,
          ),
          _allocation(
            sourceId: 'apple-pay-session-1',
            sourceType: FundingSourceType.applePay,
            amountMinor: 6500,
          ),
        ],
        sendAmountMinor: 10000,
        payoutAmountMinor: 6550000,
        payoutCurrencyCode: 'XAF',
      );

      request.validate(
        fundingSources: [
          _walletSource(availableMinor: 3500),
          _applePaySource(),
        ],
      );
      expect(request.sendCurrencyCode, 'EUR');
      expect(request.payoutCurrencyCode, 'XAF');
      expect(request.quoteId, isNull);
      expect(request.message, isNull);
    });

    test('supports single external-source funding', () {
      final request = _request(
        fundingAllocations: [
          _allocation(
            sourceId: 'sepa-method-1',
            sourceType: FundingSourceType.sepa,
            amountMinor: 10000,
          ),
        ],
      );

      request.validate();
    });

    test('requires positive send and payout amounts', () {
      expect(_request(sendAmountMinor: 0).validate, throwsArgumentError);
      expect(_request(payoutAmountMinor: 0).validate, throwsArgumentError);
      expect(_request(sendAmountMinor: -1).validate, throwsArgumentError);
    });

    test('requires valid currencies and an idempotency key', () {
      expect(_request(sendCurrencyCode: 'eur').validate, throwsArgumentError);
      expect(_request(payoutCurrencyCode: 'xAF').validate, throwsArgumentError);
      expect(_request(idempotencyKey: ' ').validate, throwsArgumentError);
    });

    test('requires non-empty allocations whose sum matches send amount', () {
      expect(_request(fundingAllocations: []).validate, throwsArgumentError);
      expect(
        _request(fundingAllocations: [_allocation(amountMinor: 9999)]).validate,
        throwsArgumentError,
      );
    });

    test('requires allocations to use the send currency', () {
      expect(
        _request(
          fundingAllocations: [
            _allocation(currencyCode: 'XAF', amountMinor: 10000),
          ],
        ).validate,
        throwsArgumentError,
      );
    });

    test('rejects duplicate funding source ids', () {
      expect(
        _request(
          fundingAllocations: [
            _allocation(amountMinor: 5000),
            _allocation(amountMinor: 5000),
          ],
        ).validate,
        throwsArgumentError,
      );
    });

    test('requires source inventory to validate wallet availability', () {
      expect(
        _request(
          fundingAllocations: [
            _allocation(
              sourceId: 'wallet-1',
              sourceType: FundingSourceType.wallet,
              amountMinor: 10000,
            ),
          ],
        ).validate,
        throwsArgumentError,
      );
    });

    test('wallet allocation cannot exceed available balance', () {
      final request = _request(
        fundingAllocations: [
          _allocation(
            sourceId: 'wallet-1',
            sourceType: FundingSourceType.wallet,
            amountMinor: 3501,
          ),
          _allocation(
            sourceId: 'apple-pay-session-1',
            sourceType: FundingSourceType.applePay,
            amountMinor: 6499,
          ),
        ],
      );

      expect(
        () => request.validate(
          fundingSources: [
            _walletSource(availableMinor: 3500),
            _applePaySource(),
          ],
        ),
        throwsArgumentError,
      );
    });

    test('wallet allocation requires an available matching wallet source', () {
      final request = _request(
        fundingAllocations: [
          _allocation(
            sourceId: 'wallet-1',
            sourceType: FundingSourceType.wallet,
            amountMinor: 10000,
          ),
        ],
      );

      expect(
        () => request.validate(
          fundingSources: [
            _walletSource(availableMinor: 10000, isAvailable: false),
          ],
        ),
        throwsArgumentError,
      );
      expect(
        () => request.validate(
          fundingSources: [
            _walletSource(availableMinor: 10000, id: 'other-wallet'),
          ],
        ),
        throwsArgumentError,
      );
    });

    test('validates optional quote id without requiring it', () {
      final allocations = [
        _allocation(
          sourceId: 'apple-pay-session-1',
          sourceType: FundingSourceType.applePay,
        ),
      ];

      expect(
        _request(fundingAllocations: allocations).validate,
        returnsNormally,
      );
      expect(
        _request(fundingAllocations: allocations, quoteId: ' ').validate,
        throwsArgumentError,
      );
    });
  });
}

MobileMoneyBeneficiary _beneficiary({
  String? beneficiaryId,
  String displayName = 'Ama Mensah',
  String phoneNumberE164 = '+237612345678',
  String countryCode = 'CM',
  String operatorCode = 'mtn-cm',
  String currencyCode = 'XAF',
}) {
  return MobileMoneyBeneficiary(
    beneficiaryId: beneficiaryId,
    displayName: displayName,
    phoneNumberE164: phoneNumberE164,
    countryCode: countryCode,
    operatorCode: operatorCode,
    currencyCode: currencyCode,
  );
}

FundingSource _walletSource({
  String id = 'wallet-1',
  int? availableMinor = 10000,
  bool isAvailable = true,
}) {
  return FundingSource(
    id: id,
    type: FundingSourceType.wallet,
    displayLabel: 'AfrikaWallet',
    currencyCode: 'EUR',
    isAvailable: isAvailable,
    availableMinor: availableMinor,
  );
}

FundingSource _applePaySource({int? availableMinor}) {
  return FundingSource(
    id: 'apple-pay-session-1',
    type: FundingSourceType.applePay,
    displayLabel: 'Apple Pay',
    currencyCode: 'EUR',
    isAvailable: true,
    availableMinor: availableMinor,
  );
}

FundingAllocation _allocation({
  String sourceId = 'source-1',
  FundingSourceType sourceType = FundingSourceType.wallet,
  int amountMinor = 10000,
  String currencyCode = 'EUR',
}) {
  return FundingAllocation(
    sourceId: sourceId,
    sourceType: sourceType,
    amountMinor: amountMinor,
    currencyCode: currencyCode,
  );
}

MobileMoneyPayoutRequest _request({
  MobileMoneyBeneficiary? beneficiary,
  int sendAmountMinor = 10000,
  String sendCurrencyCode = 'EUR',
  int payoutAmountMinor = 655000,
  String payoutCurrencyCode = 'XAF',
  List<FundingAllocation>? fundingAllocations,
  String idempotencyKey = 'payout-request-1',
  String? quoteId,
  String? message,
}) {
  return MobileMoneyPayoutRequest(
    beneficiary: beneficiary ?? _beneficiary(),
    sendAmountMinor: sendAmountMinor,
    sendCurrencyCode: sendCurrencyCode,
    payoutAmountMinor: payoutAmountMinor,
    payoutCurrencyCode: payoutCurrencyCode,
    fundingAllocations: fundingAllocations ?? [_allocation()],
    idempotencyKey: idempotencyKey,
    quoteId: quoteId,
    message: message,
  );
}

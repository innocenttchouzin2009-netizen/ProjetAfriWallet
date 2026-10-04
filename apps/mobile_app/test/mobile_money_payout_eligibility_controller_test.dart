import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/presentation/mobile_money_payout_eligibility_controller.dart';
import 'package:mobile_app/services/mobile_money_payout_repository.dart';

typedef _EligibilityLoader = Future<MobileMoneyPayoutEligibility> Function({
  required String sourceCountryCode,
  required MobileMoneyPayoutRequest payout,
  Iterable<FundingSource>? fundingSources,
});

class _FakeMobileMoneyPayoutRepository
    implements MobileMoneyPayoutRepository {
  _FakeMobileMoneyPayoutRepository(this._loader);

  final _EligibilityLoader _loader;
  int callCount = 0;

  @override
  Future<MobileMoneyPayoutEligibility> checkEligibility({
    required String sourceCountryCode,
    required MobileMoneyPayoutRequest payout,
    Iterable<FundingSource>? fundingSources,
  }) {
    callCount++;
    return _loader(
      sourceCountryCode: sourceCountryCode,
      payout: payout,
      fundingSources: fundingSources,
    );
  }
}

MobileMoneyPayoutRequest _payout() => const MobileMoneyPayoutRequest(
      beneficiary: MobileMoneyBeneficiary(
        displayName: 'Test Beneficiary',
        phoneNumberE164: '+237650000000',
        countryCode: 'CM',
        operatorCode: 'MTN_CM',
        currencyCode: 'XAF',
      ),
      sendAmountMinor: 1000,
      sendCurrencyCode: 'EUR',
      payoutAmountMinor: 650000,
      payoutCurrencyCode: 'XAF',
      fundingAllocations: <FundingAllocation>[
        FundingAllocation(
          sourceId: 'card-1',
          sourceType: FundingSourceType.paymentCard,
          amountMinor: 1000,
          currencyCode: 'EUR',
        ),
      ],
      idempotencyKey: 'payout-eligibility-test',
    );

void main() {
  test('eligible response exposes eligible presentation state', () async {
    final repository = _FakeMobileMoneyPayoutRepository(
      ({
        required String sourceCountryCode,
        required MobileMoneyPayoutRequest payout,
        Iterable<FundingSource>? fundingSources,
      }) async =>
          const MobileMoneyPayoutEligibility(isEligible: true),
    );
    final controller = MobileMoneyPayoutEligibilityController(
      repository: repository,
    );

    await controller.checkEligibility(
      sourceCountryCode: 'DE',
      payout: _payout(),
    );

    expect(
      controller.status,
      MobileMoneyPayoutEligibilityPresentationStatus.eligible,
    );
    expect(controller.isEligible, isTrue);
    expect(controller.isChecking, isFalse);
    expect(controller.failureCode, isNull);
    expect(controller.error, isNull);
    expect(repository.callCount, 1);
  });

  test('ineligible response preserves the backend failure code', () async {
    final repository = _FakeMobileMoneyPayoutRepository(
      ({
        required String sourceCountryCode,
        required MobileMoneyPayoutRequest payout,
        Iterable<FundingSource>? fundingSources,
      }) async =>
          const MobileMoneyPayoutEligibility(
            isEligible: false,
            failureCode: 'PAYOUT_CORRIDOR_DISABLED',
          ),
    );
    final controller = MobileMoneyPayoutEligibilityController(
      repository: repository,
    );

    await controller.checkEligibility(
      sourceCountryCode: 'DE',
      payout: _payout(),
    );

    expect(
      controller.status,
      MobileMoneyPayoutEligibilityPresentationStatus.ineligible,
    );
    expect(controller.isIneligible, isTrue);
    expect(controller.failureCode, 'PAYOUT_CORRIDOR_DISABLED');
    expect(controller.error, isNull);
  });

  test('repository failure exposes failed state without inventing eligibility',
      () async {
    final failure = StateError('eligibility unavailable');
    final repository = _FakeMobileMoneyPayoutRepository(
      ({
        required String sourceCountryCode,
        required MobileMoneyPayoutRequest payout,
        Iterable<FundingSource>? fundingSources,
      }) async =>
          throw failure,
    );
    final controller = MobileMoneyPayoutEligibilityController(
      repository: repository,
    );

    await controller.checkEligibility(
      sourceCountryCode: 'DE',
      payout: _payout(),
    );

    expect(
      controller.status,
      MobileMoneyPayoutEligibilityPresentationStatus.failed,
    );
    expect(controller.hasFailed, isTrue);
    expect(controller.isEligible, isFalse);
    expect(controller.isIneligible, isFalse);
    expect(controller.failureCode, isNull);
    expect(controller.error, same(failure));
  });

  test('duplicate concurrent checks are ignored while a check is in flight',
      () async {
    final completer = Completer<MobileMoneyPayoutEligibility>();
    final repository = _FakeMobileMoneyPayoutRepository(
      ({
        required String sourceCountryCode,
        required MobileMoneyPayoutRequest payout,
        Iterable<FundingSource>? fundingSources,
      }) =>
          completer.future,
    );
    final controller = MobileMoneyPayoutEligibilityController(
      repository: repository,
    );

    final first = controller.checkEligibility(
      sourceCountryCode: 'DE',
      payout: _payout(),
    );
    final duplicate = controller.checkEligibility(
      sourceCountryCode: 'DE',
      payout: _payout(),
    );

    expect(controller.isChecking, isTrue);
    expect(repository.callCount, 1);

    completer.complete(
      const MobileMoneyPayoutEligibility(isEligible: true),
    );
    await Future.wait(<Future<void>>[first, duplicate]);

    expect(repository.callCount, 1);
    expect(controller.isEligible, isTrue);
  });

  test('reset clears eligibility, failure code, and error state', () async {
    final repository = _FakeMobileMoneyPayoutRepository(
      ({
        required String sourceCountryCode,
        required MobileMoneyPayoutRequest payout,
        Iterable<FundingSource>? fundingSources,
      }) async =>
          const MobileMoneyPayoutEligibility(
            isEligible: false,
            failureCode: 'PAYOUT_OPERATOR_NOT_ACTIVATED',
          ),
    );
    final controller = MobileMoneyPayoutEligibilityController(
      repository: repository,
    );

    await controller.checkEligibility(
      sourceCountryCode: 'DE',
      payout: _payout(),
    );
    controller.reset();

    expect(
      controller.status,
      MobileMoneyPayoutEligibilityPresentationStatus.idle,
    );
    expect(controller.failureCode, isNull);
    expect(controller.error, isNull);
    expect(controller.isEligible, isFalse);
    expect(controller.isIneligible, isFalse);
    expect(controller.hasFailed, isFalse);
  });
}

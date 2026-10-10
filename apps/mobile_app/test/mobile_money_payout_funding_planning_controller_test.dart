import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_funding_intent.dart';
import 'package:mobile_app/models/mobile_money_payout_funding_plan.dart';
import 'package:mobile_app/presentation/mobile_money_payout_funding_planning_controller.dart';
import 'package:mobile_app/services/mobile_money_payout_funding_repository.dart';

typedef _FundingPlanLoader = Future<MobileMoneyPayoutFundingPlan> Function({
  required String correlationId,
  required MobileMoneyPayoutFundingIntent intent,
  required DateTime requestedAtUtc,
});

class _FakeMobileMoneyPayoutFundingRepository
    implements MobileMoneyPayoutFundingRepository {
  _FakeMobileMoneyPayoutFundingRepository(this._loader);

  final _FundingPlanLoader _loader;
  int callCount = 0;

  @override
  Future<MobileMoneyPayoutFundingPlan> planFunding({
    required String correlationId,
    required MobileMoneyPayoutFundingIntent intent,
    required DateTime requestedAtUtc,
  }) {
    callCount++;
    return _loader(
      correlationId: correlationId,
      intent: intent,
      requestedAtUtc: requestedAtUtc,
    );
  }
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

MobileMoneyPayoutFundingPlan _plan() {
  return MobileMoneyPayoutFundingPlan(
    correlationId: '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4',
    requiredAmountMinor: 10250,
    currencyCode: 'EUR',
    allocations: const <FundingAllocation>[
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
    plannedAtUtc: DateTime.utc(2026, 10, 10, 10, 30, 1),
  );
}

Future<void> _planFunding(
  MobileMoneyPayoutFundingPlanningController controller,
) {
  return controller.planFunding(
    correlationId: '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4',
    intent: _intent(),
    requestedAtUtc: DateTime.utc(2026, 10, 10, 10, 30),
  );
}

void main() {
  test('successful planning exposes ready state and backend funding plan',
      () async {
    final expected = _plan();
    final repository = _FakeMobileMoneyPayoutFundingRepository(
      ({
        required correlationId,
        required intent,
        required requestedAtUtc,
      }) async {
        expect(
          correlationId,
          '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4',
        );
        expect(intent.quoteId, 'quote-1');
        expect(intent.totalSourceDebitMinor, 10250);
        expect(intent.fundingAllocations, hasLength(2));
        expect(requestedAtUtc, DateTime.utc(2026, 10, 10, 10, 30));
        return expected;
      },
    );
    final controller = MobileMoneyPayoutFundingPlanningController(
      repository: repository,
    );

    await _planFunding(controller);

    expect(
      controller.status,
      MobileMoneyPayoutFundingPlanningPresentationStatus.ready,
    );
    expect(controller.plan, same(expected));
    expect(controller.hasPlan, isTrue);
    expect(controller.isLoading, isFalse);
    expect(controller.error, isNull);
    expect(repository.callCount, 1);
  });

  test('planning failure exposes failed state without inventing a plan',
      () async {
    final failure = StateError('funding plan unavailable');
    final repository = _FakeMobileMoneyPayoutFundingRepository(
      ({
        required correlationId,
        required intent,
        required requestedAtUtc,
      }) async =>
          throw failure,
    );
    final controller = MobileMoneyPayoutFundingPlanningController(
      repository: repository,
    );

    await _planFunding(controller);

    expect(
      controller.status,
      MobileMoneyPayoutFundingPlanningPresentationStatus.failed,
    );
    expect(controller.hasFailed, isTrue);
    expect(controller.hasPlan, isFalse);
    expect(controller.plan, isNull);
    expect(controller.error, same(failure));
  });

  test('duplicate concurrent planning requests are ignored while loading',
      () async {
    final completer = Completer<MobileMoneyPayoutFundingPlan>();
    final repository = _FakeMobileMoneyPayoutFundingRepository(
      ({
        required correlationId,
        required intent,
        required requestedAtUtc,
      }) =>
          completer.future,
    );
    final controller = MobileMoneyPayoutFundingPlanningController(
      repository: repository,
    );

    final first = _planFunding(controller);
    final duplicate = _planFunding(controller);

    expect(controller.isLoading, isTrue);
    expect(repository.callCount, 1);

    completer.complete(_plan());
    await Future.wait(<Future<void>>[first, duplicate]);

    expect(repository.callCount, 1);
    expect(controller.hasPlan, isTrue);
  });

  test('reset clears funding plan and failure state', () async {
    final repository = _FakeMobileMoneyPayoutFundingRepository(
      ({
        required correlationId,
        required intent,
        required requestedAtUtc,
      }) async =>
          _plan(),
    );
    final controller = MobileMoneyPayoutFundingPlanningController(
      repository: repository,
    );

    await _planFunding(controller);
    controller.reset();

    expect(
      controller.status,
      MobileMoneyPayoutFundingPlanningPresentationStatus.idle,
    );
    expect(controller.plan, isNull);
    expect(controller.error, isNull);
    expect(controller.hasPlan, isFalse);
    expect(controller.hasFailed, isFalse);
  });
}

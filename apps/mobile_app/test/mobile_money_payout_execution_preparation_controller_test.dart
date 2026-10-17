import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_execution_handoff.dart';
import 'package:mobile_app/models/mobile_money_payout_execution_preparation.dart';
import 'package:mobile_app/presentation/mobile_money_payout_execution_preparation_controller.dart';
import 'package:mobile_app/services/mobile_money_payout_execution_repository.dart';

typedef _Loader = Future<MobileMoneyPayoutExecutionPreparation> Function({
  required String fundingCorrelationId,
  required String idempotencyKey,
  required MobileMoneyPayoutExecutionHandoff handoff,
});

class _FakeRepository implements MobileMoneyPayoutExecutionRepository {
  _FakeRepository(this.loader);
  final _Loader loader;
  int calls = 0;

  @override
  Future<MobileMoneyPayoutExecutionPreparation> prepareExecution({
    required String fundingCorrelationId,
    required String idempotencyKey,
    required MobileMoneyPayoutExecutionHandoff handoff,
  }) {
    calls += 1;
    return loader(
      fundingCorrelationId: fundingCorrelationId,
      idempotencyKey: idempotencyKey,
      handoff: handoff,
    );
  }
}

const _fundingCorrelationId =
    '22222222-2222-4222-8222-222222222222';
const _idempotencyKey = '44444444-4444-4444-8444-444444444444';

MobileMoneyPayoutExecutionHandoff _handoff() {
  return MobileMoneyPayoutExecutionHandoff(
    quoteId: '11111111-1111-4111-8111-111111111111',
    sourceCurrencyCode: 'EUR',
    totalSourceDebitMinor: 1000,
    destinationCurrencyCode: 'XAF',
    destinationAmountMinor: 650000,
    fundingAllocations: const <FundingAllocation>[
      FundingAllocation(
        sourceId: 'wallet-1',
        sourceType: FundingSourceType.wallet,
        amountMinor: 1000,
        currencyCode: 'EUR',
      ),
    ],
    preparedAtUtc: DateTime.utc(2026, 10, 16, 12),
    quoteExpiresAtUtc: DateTime.utc(2026, 10, 16, 12, 10),
  );
}

MobileMoneyPayoutExecutionPreparation _preparation() {
  return MobileMoneyPayoutExecutionPreparation(
    bindingId: '33333333-3333-4333-8333-333333333333',
    quoteId: '11111111-1111-4111-8111-111111111111',
    fundingCorrelationId: _fundingCorrelationId,
    idempotencyKey: _idempotencyKey,
    status: 'Prepared',
    isReplay: false,
    preparedAtUtc: DateTime.utc(2026, 10, 16, 12, 0, 1),
  );
}

Future<void> _prepare(
  MobileMoneyPayoutExecutionPreparationController controller,
) {
  return controller.prepareExecution(
    fundingCorrelationId: _fundingCorrelationId,
    idempotencyKey: _idempotencyKey,
    handoff: _handoff(),
  );
}

void main() {
  test('successful preparation exposes ready state and backend binding',
      () async {
    final expected = _preparation();
    final repository = _FakeRepository(
      ({
        required fundingCorrelationId,
        required idempotencyKey,
        required handoff,
      }) async {
        expect(fundingCorrelationId, _fundingCorrelationId);
        expect(idempotencyKey, _idempotencyKey);
        expect(handoff.quoteId, '11111111-1111-4111-8111-111111111111');
        return expected;
      },
    );
    final controller = MobileMoneyPayoutExecutionPreparationController(
      repository: repository,
    );

    await _prepare(controller);

    expect(
      controller.status,
      MobileMoneyPayoutExecutionPreparationPresentationStatus.ready,
    );
    expect(controller.preparation, same(expected));
    expect(controller.hasPreparation, isTrue);
    expect(controller.error, isNull);
    expect(repository.calls, 1);
  });

  test('preparation failure exposes failed state without inventing binding',
      () async {
    final failure = StateError('unavailable');
    final repository = _FakeRepository(
      ({
        required fundingCorrelationId,
        required idempotencyKey,
        required handoff,
      }) async =>
          throw failure,
    );
    final controller = MobileMoneyPayoutExecutionPreparationController(
      repository: repository,
    );

    await _prepare(controller);

    expect(
      controller.status,
      MobileMoneyPayoutExecutionPreparationPresentationStatus.failed,
    );
    expect(controller.hasFailed, isTrue);
    expect(controller.preparation, isNull);
    expect(controller.error, same(failure));
  });

  test('duplicate concurrent preparation is ignored while loading', () async {
    final completer = Completer<MobileMoneyPayoutExecutionPreparation>();
    final repository = _FakeRepository(
      ({
        required fundingCorrelationId,
        required idempotencyKey,
        required handoff,
      }) =>
          completer.future,
    );
    final controller = MobileMoneyPayoutExecutionPreparationController(
      repository: repository,
    );

    final first = _prepare(controller);
    final duplicate = _prepare(controller);

    expect(controller.isLoading, isTrue);
    expect(repository.calls, 1);

    completer.complete(_preparation());
    await Future.wait(<Future<void>>[first, duplicate]);

    expect(repository.calls, 1);
    expect(controller.hasPreparation, isTrue);
  });

  test('reset clears preparation and failure state', () async {
    final repository = _FakeRepository(
      ({
        required fundingCorrelationId,
        required idempotencyKey,
        required handoff,
      }) async =>
          _preparation(),
    );
    final controller = MobileMoneyPayoutExecutionPreparationController(
      repository: repository,
    );

    await _prepare(controller);
    controller.reset();

    expect(
      controller.status,
      MobileMoneyPayoutExecutionPreparationPresentationStatus.idle,
    );
    expect(controller.preparation, isNull);
    expect(controller.error, isNull);
  });
}

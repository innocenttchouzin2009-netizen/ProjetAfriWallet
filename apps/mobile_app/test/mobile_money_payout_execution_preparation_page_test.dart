import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_execution_handoff.dart';
import 'package:mobile_app/models/mobile_money_payout_execution_preparation.dart';
import 'package:mobile_app/pages/mobile_money_payout_execution_preparation_page.dart';
import 'package:mobile_app/presentation/mobile_money_payout_execution_preparation_controller.dart';
import 'package:mobile_app/services/mobile_money_payout_execution_repository.dart';

class _RecordingRepository implements MobileMoneyPayoutExecutionRepository {
  _RecordingRepository({this.failFirst = false});

  final bool failFirst;
  final List<String> fundingCorrelationIds = <String>[];
  final List<String> idempotencyKeys = <String>[];
  final List<MobileMoneyPayoutExecutionHandoff> handoffs =
      <MobileMoneyPayoutExecutionHandoff>[];

  @override
  Future<MobileMoneyPayoutExecutionPreparation> prepareExecution({
    required String fundingCorrelationId,
    required String idempotencyKey,
    required MobileMoneyPayoutExecutionHandoff handoff,
  }) async {
    fundingCorrelationIds.add(fundingCorrelationId);
    idempotencyKeys.add(idempotencyKey);
    handoffs.add(handoff);

    if (failFirst && fundingCorrelationIds.length == 1) {
      throw StateError('temporary failure');
    }

    return MobileMoneyPayoutExecutionPreparation(
      bindingId: '33333333-3333-4333-8333-333333333333',
      quoteId: handoff.quoteId,
      fundingCorrelationId: fundingCorrelationId,
      idempotencyKey: idempotencyKey,
      status: 'Prepared',
      isReplay: fundingCorrelationIds.length > 1,
      preparedAtUtc: DateTime.utc(2026, 10, 16, 12, 0, 1),
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

Widget _app(
  MobileMoneyPayoutExecutionPreparationController controller, {
  required MobileMoneyPayoutExecutionHandoff handoff,
  VoidCallback? onBack,
}) {
  return MaterialApp(
    home: MobileMoneyPayoutExecutionPreparationPage(
      controller: controller,
      fundingCorrelationId: _fundingCorrelationId,
      idempotencyKey: _idempotencyKey,
      handoff: handoff,
      onBack: onBack,
    ),
  );
}

void main() {
  testWidgets('loads and presents Prepared binding without payout continuation',
      (tester) async {
    final repository = _RecordingRepository();
    final controller = MobileMoneyPayoutExecutionPreparationController(
      repository: repository,
    );
    var backCount = 0;

    await tester.pumpWidget(
      _app(
        controller,
        handoff: _handoff(),
        onBack: () => backCount += 1,
      ),
    );
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('momo-execution-preparation-ready')),
      findsOneWidget,
    );
    expect(find.text('Exécution préparée'), findsOneWidget);
    expect(find.text('Prepared'), findsOneWidget);
    expect(
      find.byKey(const Key('momo-execution-preparation-no-payout')),
      findsOneWidget,
    );
    expect(
      find.byKey(const Key('momo-execution-preparation-continue')),
      findsNothing,
    );

    await tester.tap(
      find.byKey(const Key('momo-execution-preparation-back')),
    );
    expect(backCount, 1);
  });

  testWidgets('retry reuses the exact same binding inputs', (tester) async {
    final repository = _RecordingRepository(failFirst: true);
    final controller = MobileMoneyPayoutExecutionPreparationController(
      repository: repository,
    );
    final handoff = _handoff();

    await tester.pumpWidget(_app(controller, handoff: handoff));
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('momo-execution-preparation-failed')),
      findsOneWidget,
    );
    expect(repository.fundingCorrelationIds, <String>[_fundingCorrelationId]);
    expect(repository.idempotencyKeys, <String>[_idempotencyKey]);
    expect(identical(repository.handoffs.single, handoff), isTrue);

    await tester.tap(
      find.byKey(const Key('momo-execution-preparation-retry')),
    );
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('momo-execution-preparation-ready')),
      findsOneWidget,
    );
    expect(
      repository.fundingCorrelationIds,
      <String>[_fundingCorrelationId, _fundingCorrelationId],
    );
    expect(
      repository.idempotencyKeys,
      <String>[_idempotencyKey, _idempotencyKey],
    );
    expect(identical(repository.handoffs[0], repository.handoffs[1]), isTrue);
    expect(find.text('Relecture idempotente confirmée'), findsOneWidget);
  });
}

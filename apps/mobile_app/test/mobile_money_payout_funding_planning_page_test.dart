import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_funding_intent.dart';
import 'package:mobile_app/models/mobile_money_payout_funding_plan.dart';
import 'package:mobile_app/pages/mobile_money_payout_funding_planning_page.dart';
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

const _correlationId = '7d8fba6d-d06f-4bd5-98da-cb71d07bfef4';

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
    correlationId: _correlationId,
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
    plannedAtUtc: DateTime.utc(2026, 10, 10, 11, 30),
  );
}

Widget _app(
  MobileMoneyPayoutFundingPlanningController controller, {
  ValueChanged<MobileMoneyPayoutFundingPlan>? onContinue,
  VoidCallback? onBack,
}) {
  return MaterialApp(
    home: MobileMoneyPayoutFundingPlanningPage(
      controller: controller,
      correlationId: _correlationId,
      intent: _intent(),
      requestedAtUtc: DateTime.utc(2026, 10, 10, 11, 29),
      onContinue: onContinue,
      onBack: onBack,
    ),
  );
}

void main() {
  testWidgets(
    'loads and presents the backend-confirmed funding plan without payout',
    (tester) async {
      final completer = Completer<MobileMoneyPayoutFundingPlan>();
      final repository = _FakeMobileMoneyPayoutFundingRepository(
        ({
          required correlationId,
          required intent,
          required requestedAtUtc,
        }) {
          expect(correlationId, _correlationId);
          expect(intent.quoteId, 'quote-1');
          expect(intent.totalSourceDebitMinor, 10250);
          expect(intent.fundingAllocations, hasLength(2));
          expect(requestedAtUtc, DateTime.utc(2026, 10, 10, 11, 29));
          return completer.future;
        },
      );
      final controller = MobileMoneyPayoutFundingPlanningController(
        repository: repository,
      );
      MobileMoneyPayoutFundingPlan? continuedPlan;

      await tester.pumpWidget(
        _app(
          controller,
          onContinue: (plan) => continuedPlan = plan,
        ),
      );
      await tester.pump();

      expect(
        find.byKey(const Key('momo-funding-plan-loading')),
        findsOneWidget,
      );
      expect(repository.callCount, 1);

      final plan = _plan();
      completer.complete(plan);
      await tester.pumpAndSettle();

      expect(
        find.byKey(const Key('momo-funding-plan-ready')),
        findsOneWidget,
      );
      expect(
        find.byKey(const Key('momo-funding-plan-total')),
        findsOneWidget,
      );
      expect(
        find.byKey(const Key('momo-funding-plan-allocations')),
        findsOneWidget,
      );
      expect(find.text('Solde AfrikaWallet'), findsOneWidget);
      expect(find.text('Google Pay'), findsOneWidget);

      await tester.drag(
        find.byKey(const Key('momo-funding-plan-ready')),
        const Offset(0, -500),
      );
      await tester.pumpAndSettle();

      expect(
        find.byKey(const Key('momo-funding-plan-no-payout')),
        findsOneWidget,
      );
      expect(
        find.byKey(const Key('momo-funding-plan-continue')),
        findsOneWidget,
      );
      expect(continuedPlan, isNull);

      await tester.tap(
        find.byKey(const Key('momo-funding-plan-continue')),
      );

      expect(continuedPlan, same(plan));
      expect(repository.callCount, 1);
    },
  );

  testWidgets(
    'retry requests planning again and back remains presentation-only',
    (tester) async {
      var shouldFail = true;
      final retryCompleter = Completer<MobileMoneyPayoutFundingPlan>();
      final repository = _FakeMobileMoneyPayoutFundingRepository(
        ({
          required correlationId,
          required intent,
          required requestedAtUtc,
        }) {
          if (shouldFail) {
            return Future<MobileMoneyPayoutFundingPlan>.error(
              StateError('funding planning unavailable'),
            );
          }
          return retryCompleter.future;
        },
      );
      final controller = MobileMoneyPayoutFundingPlanningController(
        repository: repository,
      );
      var backCount = 0;

      await tester.pumpWidget(
        _app(
          controller,
          onBack: () => backCount++,
        ),
      );
      await tester.pumpAndSettle();

      expect(
        find.byKey(const Key('momo-funding-plan-failed')),
        findsOneWidget,
      );
      expect(repository.callCount, 1);

      await tester.tap(
        find.byKey(const Key('momo-funding-plan-back')),
      );
      expect(backCount, 1);
      expect(repository.callCount, 1);

      shouldFail = false;
      await tester.tap(
        find.byKey(const Key('momo-funding-plan-retry')),
      );
      await tester.pump();

      expect(
        find.byKey(const Key('momo-funding-plan-loading')),
        findsOneWidget,
      );
      expect(repository.callCount, 2);

      retryCompleter.complete(_plan());
      await tester.pumpAndSettle();

      expect(
        find.byKey(const Key('momo-funding-plan-ready')),
        findsOneWidget,
      );
      expect(repository.callCount, 2);
    },
  );
}

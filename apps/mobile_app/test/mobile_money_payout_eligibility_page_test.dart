import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/pages/mobile_money_payout_eligibility_page.dart';
import 'package:mobile_app/presentation/mobile_money_payout_eligibility_controller.dart';
import 'package:mobile_app/services/mobile_money_payout_repository.dart';

typedef _EligibilityLoader = Future<MobileMoneyPayoutEligibility> Function();

class _FakeRepository implements MobileMoneyPayoutRepository {
  _FakeRepository(this.loader);

  final _EligibilityLoader loader;
  int callCount = 0;

  @override
  Future<MobileMoneyPayoutEligibility> checkEligibility({
    required String sourceCountryCode,
    required MobileMoneyPayoutRequest payout,
    Iterable<FundingSource>? fundingSources,
  }) {
    callCount++;
    return loader();
  }
}

MobileMoneyPayoutRequest _payout() => const MobileMoneyPayoutRequest(
      beneficiary: MobileMoneyBeneficiary(
        displayName: 'Beneficiary',
        phoneNumberE164: '+237650000000',
        countryCode: 'CM',
        operatorCode: 'MTN_CM',
        currencyCode: 'XAF',
      ),
      sendAmountMinor: 1000,
      sendCurrencyCode: 'EUR',
      payoutAmountMinor: 650000,
      payoutCurrencyCode: 'XAF',
      fundingAllocations: <FundingAllocation>[],
      idempotencyKey: 'eligibility-ui-test',
    );

Widget _app(
  MobileMoneyPayoutEligibilityController controller, {
  VoidCallback? onContinue,
}) {
  return MaterialApp(
    home: MobileMoneyPayoutEligibilityPage(
      controller: controller,
      sourceCountryCode: 'DE',
      payout: _payout(),
      onEligibleContinue: onContinue,
    ),
  );
}

void main() {
  testWidgets('shows checking then eligible without executing a payout',
      (tester) async {
    final completer = Completer<MobileMoneyPayoutEligibility>();
    final repository = _FakeRepository(() => completer.future);
    final controller =
        MobileMoneyPayoutEligibilityController(repository: repository);
    var continued = false;

    await tester.pumpWidget(
      _app(controller, onContinue: () => continued = true),
    );
    await tester.pump();

    expect(find.byKey(const Key('momo-eligibility-checking')), findsOneWidget);
    expect(repository.callCount, 1);

    completer.complete(
      const MobileMoneyPayoutEligibility(isEligible: true),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('momo-eligibility-eligible')), findsOneWidget);
    expect(find.byKey(const Key('momo-eligibility-continue')), findsOneWidget);
    expect(continued, isFalse);

    await tester.tap(find.byKey(const Key('momo-eligibility-continue')));
    expect(continued, isTrue);
  });

  testWidgets('renders ineligible and preserves backend failure code',
      (tester) async {
    final repository = _FakeRepository(
      () async => const MobileMoneyPayoutEligibility(
        isEligible: false,
        failureCode: 'PAYOUT_CORRIDOR_DISABLED',
      ),
    );
    final controller =
        MobileMoneyPayoutEligibilityController(repository: repository);

    await tester.pumpWidget(_app(controller));
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('momo-eligibility-ineligible')),
      findsOneWidget,
    );
    expect(find.text('Code : PAYOUT_CORRIDOR_DISABLED'), findsOneWidget);
  });

  testWidgets('renders failed state and retry re-checks eligibility',
      (tester) async {
    final retryCompleter = Completer<MobileMoneyPayoutEligibility>();
    var shouldFail = true;
    final repository = _FakeRepository(() {
      if (shouldFail) {
        return Future<MobileMoneyPayoutEligibility>.error(
          StateError('temporarily unavailable'),
        );
      }
      return retryCompleter.future;
    });
    final controller =
        MobileMoneyPayoutEligibilityController(repository: repository);

    await tester.pumpWidget(_app(controller));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('momo-eligibility-failed')), findsOneWidget);
    expect(repository.callCount, 1);

    shouldFail = false;
    await tester.tap(find.byKey(const Key('momo-eligibility-retry-failed')));
    await tester.pump();

    expect(find.byKey(const Key('momo-eligibility-checking')), findsOneWidget);
    expect(repository.callCount, 2);

    retryCompleter.complete(
      const MobileMoneyPayoutEligibility(isEligible: true),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('momo-eligibility-eligible')), findsOneWidget);
    expect(repository.callCount, 2);
  });
}

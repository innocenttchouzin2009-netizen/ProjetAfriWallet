import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_funding_intent.dart';
import 'package:mobile_app/models/mobile_money_payout_quote.dart';
import 'package:mobile_app/pages/mobile_money_payout_funding_selection_page.dart';
import 'package:mobile_app/presentation/mobile_money_payout_funding_selection_controller.dart';

MobileMoneyPayoutQuote _quote() => MobileMoneyPayoutQuote(
      quoteId: 'quote-1',
      sourceCurrencyCode: 'EUR',
      sourceAmountMinor: 1000,
      fees: const <MobileMoneyPayoutQuoteFee>[
        MobileMoneyPayoutQuoteFee(
          code: 'TRANSFER_FEE',
          amountMinor: 50,
          currencyCode: 'EUR',
        ),
      ],
      totalFeeMinor: 50,
      totalSourceDebitMinor: 1050,
      destinationCurrencyCode: 'XAF',
      destinationAmountMinor: 650000,
      fxRate: 650,
      createdAtUtc: DateTime.utc(2026, 10, 9, 14),
      expiresAtUtc: DateTime.utc(2026, 10, 9, 14, 10),
    );

FundingSource _wallet({required int availableMinor}) => FundingSource(
      id: 'wallet-eur',
      type: FundingSourceType.wallet,
      displayLabel: 'Solde AfrikaWallet',
      currencyCode: 'EUR',
      isAvailable: true,
      availableMinor: availableMinor,
    );

const FundingSource _applePay = FundingSource(
  id: 'apple-pay',
  type: FundingSourceType.applePay,
  displayLabel: 'Apple Pay',
  currencyCode: 'EUR',
  isAvailable: true,
);

Widget _app(
  MobileMoneyPayoutFundingSelectionController controller, {
  ValueChanged<MobileMoneyPayoutFundingIntent>? onContinue,
}) {
  return MaterialApp(
    home: MobileMoneyPayoutFundingSelectionPage(
      controller: controller,
      onContinue: onContinue,
    ),
  );
}

void main() {
  testWidgets('presents wallet, external, and automatic split funding',
      (tester) async {
    final controller = MobileMoneyPayoutFundingSelectionController(
      quote: _quote(),
      fundingSources: <FundingSource>[
        _wallet(availableMinor: 400),
        _applePay,
      ],
    );
    MobileMoneyPayoutFundingIntent? continuedIntent;

    await tester.pumpWidget(
      _app(
        controller,
        onContinue: (intent) => continuedIntent = intent,
      ),
    );

    expect(find.byKey(const Key('momo-funding-total')), findsOneWidget);
    expect(find.byKey(const Key('momo-funding-wallet')), findsOneWidget);
    expect(
      find.byKey(const Key('momo-funding-external-apple-pay')),
      findsOneWidget,
    );
    expect(
      find.byKey(const Key('momo-funding-split-apple-pay')),
      findsOneWidget,
    );
    await tester.scrollUntilVisible(
      find.byKey(const Key('momo-funding-no-payout')),
      150,
    );
    expect(find.byKey(const Key('momo-funding-no-payout')), findsOneWidget);

    await tester.scrollUntilVisible(
      find.byKey(const Key('momo-funding-continue')),
      150,
    );
    final continueButton = tester.widget<FilledButton>(
      find.byKey(const Key('momo-funding-continue')),
    );
    expect(continueButton.onPressed, isNull);

    await tester.scrollUntilVisible(
      find.byKey(const Key('momo-funding-split-apple-pay')),
      -150,
    );
    await tester.tap(
      find.byKey(const Key('momo-funding-split-apple-pay')),
    );
    await tester.pump();

    await tester.scrollUntilVisible(
      find.byKey(const Key('momo-funding-selected')),
      150,
    );
    expect(find.byKey(const Key('momo-funding-selected')), findsOneWidget);
    expect(controller.selectedIntent, isNotNull);
    expect(controller.selectedIntent!.fundingAllocations, hasLength(2));

    await tester.scrollUntilVisible(
      find.byKey(const Key('momo-funding-continue')),
      150,
    );
    await tester.tap(find.byKey(const Key('momo-funding-continue')));

    expect(continuedIntent, same(controller.selectedIntent));
    expect(continuedIntent!.fundingAllocations[0].amountMinor, 400);
    expect(continuedIntent!.fundingAllocations[1].amountMinor, 650);
  });

  testWidgets('wallet-only becomes selectable when balance covers total',
      (tester) async {
    final controller = MobileMoneyPayoutFundingSelectionController(
      quote: _quote(),
      fundingSources: <FundingSource>[
        _wallet(availableMinor: 2000),
        _applePay,
      ],
    );

    await tester.pumpWidget(_app(controller));

    expect(
      find.byKey(const Key('momo-funding-split-apple-pay')),
      findsNothing,
    );

    await tester.tap(find.byKey(const Key('momo-funding-wallet')));
    await tester.pump();

    expect(
      controller.selectedMode,
      MobileMoneyPayoutFundingSelectionMode.walletOnly,
    );
    expect(
      controller.selectedIntent!.fundingAllocations.single.amountMinor,
      1050,
    );
  });

  testWidgets('external-only selection does not launch a payment',
      (tester) async {
    final controller = MobileMoneyPayoutFundingSelectionController(
      quote: _quote(),
      fundingSources: <FundingSource>[
        _wallet(availableMinor: 400),
        _applePay,
      ],
    );

    await tester.pumpWidget(_app(controller));

    await tester.tap(
      find.byKey(const Key('momo-funding-external-apple-pay')),
    );
    await tester.pump();

    expect(
      controller.selectedMode,
      MobileMoneyPayoutFundingSelectionMode.externalOnly,
    );
    expect(controller.selectedIntent!.fundingAllocations, hasLength(1));
    await tester.scrollUntilVisible(
      find.byKey(const Key('momo-funding-no-payout')),
      150,
    );
    expect(find.byKey(const Key('momo-funding-no-payout')), findsOneWidget);
  });
}

import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_quote.dart';
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

FundingSource _wallet({int availableMinor = 1050}) => FundingSource(
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

const FundingSource _unavailableCard = FundingSource(
  id: 'card-unavailable',
  type: FundingSourceType.paymentCard,
  displayLabel: 'Carte indisponible',
  currencyCode: 'EUR',
  isAvailable: false,
);

void main() {
  test('wallet-only selection builds a validated funding intent', () {
    final controller = MobileMoneyPayoutFundingSelectionController(
      quote: _quote(),
      fundingSources: <FundingSource>[_wallet(), _applePay],
    );

    controller.selectWalletOnly();

    expect(
      controller.selectedMode,
      MobileMoneyPayoutFundingSelectionMode.walletOnly,
    );
    expect(controller.selectedExternalSourceId, isNull);
    expect(controller.selectedIntent, isNotNull);
    expect(controller.selectedIntent!.quoteId, 'quote-1');
    expect(controller.selectedIntent!.totalSourceDebitMinor, 1050);
    expect(controller.selectedIntent!.fundingAllocations, hasLength(1));
    expect(
      controller.selectedIntent!.fundingAllocations.single.sourceType,
      FundingSourceType.wallet,
    );
    expect(
      controller.selectedIntent!.fundingAllocations.single.amountMinor,
      1050,
    );
  });

  test('external-only selection funds the entire backend quote debit', () {
    final controller = MobileMoneyPayoutFundingSelectionController(
      quote: _quote(),
      fundingSources: <FundingSource>[_wallet(availableMinor: 400), _applePay],
    );

    controller.selectExternal(_applePay);

    expect(
      controller.selectedMode,
      MobileMoneyPayoutFundingSelectionMode.externalOnly,
    );
    expect(controller.selectedExternalSourceId, 'apple-pay');
    expect(controller.selectedIntent!.fundingAllocations, hasLength(1));
    expect(
      controller.selectedIntent!.fundingAllocations.single.sourceType,
      FundingSourceType.applePay,
    );
    expect(
      controller.selectedIntent!.fundingAllocations.single.amountMinor,
      1050,
    );
  });

  test('split selection uses available wallet balance and external remainder',
      () {
    final controller = MobileMoneyPayoutFundingSelectionController(
      quote: _quote(),
      fundingSources: <FundingSource>[_wallet(availableMinor: 400), _applePay],
    );

    expect(controller.canUseWalletOnly, isFalse);
    expect(controller.canUseSplit, isTrue);
    expect(controller.splitWalletAmountMinor, 400);
    expect(controller.splitExternalAmountMinor, 650);

    controller.selectSplit(_applePay);

    final allocations = controller.selectedIntent!.fundingAllocations;
    expect(
      controller.selectedMode,
      MobileMoneyPayoutFundingSelectionMode.split,
    );
    expect(controller.selectedExternalSourceId, 'apple-pay');
    expect(allocations, hasLength(2));
    expect(allocations[0].sourceType, FundingSourceType.wallet);
    expect(allocations[0].amountMinor, 400);
    expect(allocations[1].sourceType, FundingSourceType.applePay);
    expect(allocations[1].amountMinor, 650);
  });

  test('unavailable external sources are not selectable', () {
    final controller = MobileMoneyPayoutFundingSelectionController(
      quote: _quote(),
      fundingSources: <FundingSource>[
        _wallet(availableMinor: 400),
        _applePay,
        _unavailableCard,
      ],
    );

    expect(
      controller.availableExternalSources.map((source) => source.id),
      <String>['apple-pay'],
    );
    expect(
      () => controller.selectExternal(_unavailableCard),
      throwsStateError,
    );
  });

  test('clearSelection removes the presentation choice', () {
    final controller = MobileMoneyPayoutFundingSelectionController(
      quote: _quote(),
      fundingSources: <FundingSource>[_wallet(), _applePay],
    );

    controller.selectExternal(_applePay);
    controller.clearSelection();

    expect(controller.selectedMode, isNull);
    expect(controller.selectedIntent, isNull);
    expect(controller.selectedExternalSourceId, isNull);
  });
}

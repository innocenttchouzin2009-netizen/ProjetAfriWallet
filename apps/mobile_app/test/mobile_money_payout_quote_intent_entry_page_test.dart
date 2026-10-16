import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_beneficiary_lookup.dart';
import 'package:mobile_app/models/mobile_money_beneficiary_operator.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_quote.dart';
import 'package:mobile_app/models/mobile_money_payout_quote_intent.dart';
import 'package:mobile_app/models/wallet_balance.dart';
import 'package:mobile_app/pages/mobile_money_payout_funding_selection_page.dart';
import 'package:mobile_app/pages/mobile_money_payout_quote_intent_entry_page.dart';
import 'package:mobile_app/presentation/mobile_money_payout_funding_selection_controller.dart';
import 'package:mobile_app/services/mobile_money_beneficiary_lookup_repository.dart';
import 'package:mobile_app/services/mobile_money_payout_quote_repository.dart';

class _CapturingQuoteRepository implements MobileMoneyPayoutQuoteRepository {
  _CapturingQuoteRepository({
    this.quoteSourceCurrencyCode,
  });

  final String? quoteSourceCurrencyCode;
  MobileMoneyPayoutQuoteIntent? lastIntent;
  int calls = 0;

  @override
  Future<MobileMoneyPayoutQuote> createQuoteFromIntent(
    MobileMoneyPayoutQuoteIntent intent,
  ) async {
    calls += 1;
    lastIntent = intent;
    return MobileMoneyPayoutQuote(
      quoteId: 'quote-entry',
      sourceCurrencyCode:
          quoteSourceCurrencyCode ?? intent.sourceCurrencyCode,
      sourceAmountMinor: intent.sourceAmountMinor,
      fees: const <MobileMoneyPayoutQuoteFee>[],
      totalFeeMinor: 0,
      totalSourceDebitMinor: intent.sourceAmountMinor,
      destinationCurrencyCode: intent.destinationCurrencyCode,
      destinationAmountMinor: 808000,
      fxRate: 655,
      createdAtUtc: DateTime.utc(2026, 10, 16, 10),
      expiresAtUtc: DateTime.utc(2099),
    );
  }

  @override
  Future<MobileMoneyPayoutQuote> createQuote({
    required String sourceCountryCode,
    required String sourceCurrencyCode,
    required String destinationCountryCode,
    required String destinationCurrencyCode,
    required String operatorCode,
    required int sourceAmountMinor,
  }) {
    throw StateError('The entry flow must use a validated quote intent.');
  }
}

const _resolvedBeneficiary = MobileMoneyBeneficiarySearchResult(
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

const _sourceWallet = WalletBalance(
  walletId: 'wallet-eur-primary',
  currency: 'EUR',
  availableMinor: 25000,
  status: 'ACTIVE',
  countryCode: 'DE',
);

Widget _app({
  required WalletBalance sourceWallet,
  required MobileMoneyBeneficiarySearchResult beneficiary,
  required MobileMoneyPayoutQuoteRepository quoteRepository,
}) {
  return MaterialApp(
    home: MobileMoneyPayoutQuoteIntentEntryPage(
      sourceWallet: sourceWallet,
      beneficiary: beneficiary,
      quoteRepository: quoteRepository,
    ),
  );
}

Future<void> _useLargeSurface(WidgetTester tester) async {
  await tester.binding.setSurfaceSize(const Size(900, 1400));
  addTearDown(() => tester.binding.setSurfaceSize(null));
}

void main() {
  testWidgets(
    'builds a validated quote intent from the authoritative source wallet without floating point parsing',
    (tester) async {
      await _useLargeSurface(tester);
      final repository = _CapturingQuoteRepository();
      await tester.pumpWidget(
        _app(
          sourceWallet: _sourceWallet,
          beneficiary: _resolvedBeneficiary,
          quoteRepository: repository,
        ),
      );

      expect(find.text('wallet-eur-primary'), findsOneWidget);
      expect(find.text('DE · EUR'), findsOneWidget);
      expect(find.text('Ada N.'), findsOneWidget);

      await tester.enterText(
        find.byKey(const Key('momo-quote-intent-amount')),
        '12,34',
      );
      await tester.tap(
        find.byKey(const Key('momo-quote-intent-continue')),
      );
      await tester.pumpAndSettle();

      final intent = repository.lastIntent;
      expect(repository.calls, 1);
      expect(intent, isNotNull);
      expect(intent!.sourceCountryCode, 'DE');
      expect(intent.sourceCurrencyCode, 'EUR');
      expect(intent.destinationCurrencyCode, 'XAF');
      expect(intent.sourceAmountMinor, 1234);
      expect(intent.beneficiary.normalizedPhoneNumber, '+237670123456');
      expect(intent.beneficiary.countryCode, 'CM');
      expect(intent.beneficiary.operatorCode, 'MTN_CM');
      expect(intent.beneficiary.accountHolderName, 'Ada N.');
      expect(find.text('Votre estimation'), findsOneWidget);
    },
  );

  testWidgets('rejects invalid, zero and over-precision amounts', (tester) async {
    await _useLargeSurface(tester);
    final repository = _CapturingQuoteRepository();
    await tester.pumpWidget(
      _app(
        sourceWallet: _sourceWallet,
        beneficiary: _resolvedBeneficiary,
        quoteRepository: repository,
      ),
    );

    for (final value in <String>['0', '-1', '12.345', 'abc']) {
      await tester.enterText(
        find.byKey(const Key('momo-quote-intent-amount')),
        value,
      );
      await tester.tap(
        find.byKey(const Key('momo-quote-intent-continue')),
      );
      await tester.pump();
      expect(repository.calls, 0);
      expect(find.textContaining('strictement positif'), findsOneWidget);
    }
  });

  testWidgets('fails closed when the primary wallet has no country context',
      (tester) async {
    await _useLargeSurface(tester);
    final repository = _CapturingQuoteRepository();
    await tester.pumpWidget(
      _app(
        sourceWallet: const WalletBalance(
          walletId: 'wallet-missing-country',
          currency: 'EUR',
          availableMinor: 25000,
          status: 'ACTIVE',
        ),
        beneficiary: _resolvedBeneficiary,
        quoteRepository: repository,
      ),
    );

    expect(
      find.byKey(const Key('momo-quote-intent-source-unavailable')),
      findsOneWidget,
    );
    final button = tester.widget<FilledButton>(
      find.byKey(const Key('momo-quote-intent-continue')),
    );
    expect(button.onPressed, isNull);
    expect(repository.calls, 0);
  });

  testWidgets('fails closed for an unresolved beneficiary', (tester) async {
    await _useLargeSurface(tester);
    final repository = _CapturingQuoteRepository();
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

    await tester.pumpWidget(
      _app(
        sourceWallet: _sourceWallet,
        beneficiary: unresolved,
        quoteRepository: repository,
      ),
    );

    expect(find.textContaining('entièrement vérifié'), findsOneWidget);
    final button = tester.widget<FilledButton>(
      find.byKey(const Key('momo-quote-intent-continue')),
    );
    expect(button.onPressed, isNull);
    expect(repository.calls, 0);
  });

  testWidgets(
    'fresh quote continues to funding selection with the exact authoritative wallet only',
    (tester) async {
      await _useLargeSurface(tester);
      final repository = _CapturingQuoteRepository();

      await tester.pumpWidget(
        _app(
          sourceWallet: _sourceWallet,
          beneficiary: _resolvedBeneficiary,
          quoteRepository: repository,
        ),
      );
      await tester.enterText(
        find.byKey(const Key('momo-quote-intent-amount')),
        '12.34',
      );
      await tester.tap(
        find.byKey(const Key('momo-quote-intent-continue')),
      );
      await tester.pumpAndSettle();

      await tester.drag(
        find.byKey(const Key('momo-quote-ready')),
        const Offset(0, -600),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('momo-quote-continue')));
      await tester.pumpAndSettle();

      expect(
        find.byType(MobileMoneyPayoutFundingSelectionPage),
        findsOneWidget,
      );
      final fundingPage = tester.widget<MobileMoneyPayoutFundingSelectionPage>(
        find.byType(MobileMoneyPayoutFundingSelectionPage),
      );
      final controller = fundingPage.controller;
      final walletSource = controller.fundingSources.single;

      expect(walletSource.id, 'wallet-eur-primary');
      expect(walletSource.type, FundingSourceType.wallet);
      expect(walletSource.displayLabel, 'Solde AfrikaWallet');
      expect(walletSource.currencyCode, 'EUR');
      expect(walletSource.isAvailable, isTrue);
      expect(walletSource.availableMinor, 25000);
      expect(controller.availableExternalSources, isEmpty);
      expect(controller.canUseSplit, isFalse);
      expect(controller.canUseWalletOnly, isTrue);
      expect(
        find.byKey(const Key('momo-funding-continue')),
        findsNothing,
      );

      await tester.tap(find.byKey(const Key('momo-funding-wallet')));
      await tester.pump();
      expect(
        controller.selectedMode,
        MobileMoneyPayoutFundingSelectionMode.walletOnly,
      );
      expect(
        controller.selectedIntent!.fundingAllocations.single.sourceId,
        'wallet-eur-primary',
      );

      await tester.tap(find.byKey(const Key('momo-funding-back')));
      await tester.pumpAndSettle();
      expect(find.byKey(const Key('momo-quote-ready')), findsOneWidget);
    },
  );

  testWidgets(
    'insufficient wallet remains fail closed without external or split funding',
    (tester) async {
      await _useLargeSurface(tester);
      final repository = _CapturingQuoteRepository();
      const lowBalanceWallet = WalletBalance(
        walletId: 'wallet-low-eur',
        currency: 'EUR',
        availableMinor: 500,
        status: 'ACTIVE',
        countryCode: 'DE',
      );

      await tester.pumpWidget(
        _app(
          sourceWallet: lowBalanceWallet,
          beneficiary: _resolvedBeneficiary,
          quoteRepository: repository,
        ),
      );
      await tester.enterText(
        find.byKey(const Key('momo-quote-intent-amount')),
        '10.00',
      );
      await tester.tap(
        find.byKey(const Key('momo-quote-intent-continue')),
      );
      await tester.pumpAndSettle();
      await tester.drag(
        find.byKey(const Key('momo-quote-ready')),
        const Offset(0, -600),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('momo-quote-continue')));
      await tester.pumpAndSettle();

      final fundingPage = tester.widget<MobileMoneyPayoutFundingSelectionPage>(
        find.byType(MobileMoneyPayoutFundingSelectionPage),
      );
      final controller = fundingPage.controller;
      expect(controller.canUseWalletOnly, isFalse);
      expect(controller.availableExternalSources, isEmpty);
      expect(controller.canUseSplit, isFalse);
      expect(
        tester.widget<ListTile>(
          find.byKey(const Key('momo-funding-wallet')),
        ).enabled,
        isFalse,
      );
      expect(
        find.byKey(const Key('momo-funding-continue')),
        findsNothing,
      );
    },
  );

  testWidgets(
    'quote currency mismatch fails closed before funding selection',
    (tester) async {
      await _useLargeSurface(tester);
      final repository = _CapturingQuoteRepository(
        quoteSourceCurrencyCode: 'USD',
      );

      await tester.pumpWidget(
        _app(
          sourceWallet: _sourceWallet,
          beneficiary: _resolvedBeneficiary,
          quoteRepository: repository,
        ),
      );
      await tester.enterText(
        find.byKey(const Key('momo-quote-intent-amount')),
        '10.00',
      );
      await tester.tap(
        find.byKey(const Key('momo-quote-intent-continue')),
      );
      await tester.pumpAndSettle();
      await tester.drag(
        find.byKey(const Key('momo-quote-ready')),
        const Offset(0, -600),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('momo-quote-continue')));
      await tester.pump();

      expect(
        find.byType(MobileMoneyPayoutFundingSelectionPage),
        findsNothing,
      );
      expect(
        find.byKey(const Key('momo-funding-source-invalid-snackbar')),
        findsOneWidget,
      );
    },
  );

  testWidgets(
    'blank inactive or negative-balance source wallet fails closed before quote',
    (tester) async {
      await _useLargeSurface(tester);
      final repository = _CapturingQuoteRepository();
      const invalidWallets = <WalletBalance>[
        WalletBalance(
          walletId: '   ',
          currency: 'EUR',
          availableMinor: 25000,
          status: 'ACTIVE',
          countryCode: 'DE',
        ),
        WalletBalance(
          walletId: 'wallet-inactive',
          currency: 'EUR',
          availableMinor: 25000,
          status: 'INACTIVE',
          countryCode: 'DE',
        ),
        WalletBalance(
          walletId: 'wallet-negative',
          currency: 'EUR',
          availableMinor: -1,
          status: 'ACTIVE',
          countryCode: 'DE',
        ),
      ];

      for (final wallet in invalidWallets) {
        await tester.pumpWidget(
          _app(
            sourceWallet: wallet,
            beneficiary: _resolvedBeneficiary,
            quoteRepository: repository,
          ),
        );
        await tester.pump();

        expect(
          find.byKey(const Key('momo-quote-intent-source-unavailable')),
          findsOneWidget,
        );
        expect(
          tester.widget<FilledButton>(
            find.byKey(const Key('momo-quote-intent-continue')),
          ).onPressed,
          isNull,
        );
        expect(repository.calls, 0);
      }
    },
  );

}

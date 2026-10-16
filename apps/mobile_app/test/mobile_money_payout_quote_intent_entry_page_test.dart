import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_beneficiary_lookup.dart';
import 'package:mobile_app/models/mobile_money_beneficiary_operator.dart';
import 'package:mobile_app/models/mobile_money_payout.dart';
import 'package:mobile_app/models/mobile_money_payout_execution_handoff.dart';
import 'package:mobile_app/models/mobile_money_payout_execution_preparation.dart';
import 'package:mobile_app/models/mobile_money_payout_funding_intent.dart';
import 'package:mobile_app/models/mobile_money_payout_funding_plan.dart';
import 'package:mobile_app/models/mobile_money_payout_quote.dart';
import 'package:mobile_app/models/mobile_money_payout_quote_intent.dart';
import 'package:mobile_app/models/wallet_balance.dart';
import 'package:mobile_app/pages/mobile_money_payout_execution_preparation_page.dart';
import 'package:mobile_app/pages/mobile_money_payout_funding_planning_page.dart';
import 'package:mobile_app/pages/mobile_money_payout_funding_selection_page.dart';
import 'package:mobile_app/pages/mobile_money_payout_quote_intent_entry_page.dart';
import 'package:mobile_app/presentation/mobile_money_payout_funding_selection_controller.dart';
import 'package:mobile_app/services/mobile_money_beneficiary_lookup_repository.dart';
import 'package:mobile_app/services/mobile_money_payout_execution_repository.dart';
import 'package:mobile_app/services/mobile_money_payout_funding_repository.dart';
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


class _CapturingFundingRepository implements MobileMoneyPayoutFundingRepository {
  _CapturingFundingRepository({this.failFirst = false});

  final bool failFirst;
  final List<String> correlationIds = <String>[];
  final List<DateTime> requestedAtUtcValues = <DateTime>[];
  final List<MobileMoneyPayoutFundingIntent> intents =
      <MobileMoneyPayoutFundingIntent>[];

  @override
  Future<MobileMoneyPayoutFundingPlan> planFunding({
    required String correlationId,
    required MobileMoneyPayoutFundingIntent intent,
    required DateTime requestedAtUtc,
  }) async {
    correlationIds.add(correlationId);
    requestedAtUtcValues.add(requestedAtUtc);
    intents.add(intent);

    if (failFirst && correlationIds.length == 1) {
      throw StateError('temporary planning failure');
    }

    return MobileMoneyPayoutFundingPlan(
      correlationId: correlationId,
      requiredAmountMinor: intent.totalSourceDebitMinor,
      currencyCode: intent.sourceCurrencyCode,
      allocations: intent.fundingAllocations,
      plannedAtUtc: requestedAtUtc.add(const Duration(seconds: 1)),
    );
  }
}


class _CapturingExecutionRepository
    implements MobileMoneyPayoutExecutionRepository {
  _CapturingExecutionRepository({this.failFirst = false});

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
      throw StateError('temporary execution preparation failure');
    }

    return MobileMoneyPayoutExecutionPreparation(
      bindingId: '33333333-3333-4333-8333-333333333333',
      quoteId: handoff.quoteId,
      fundingCorrelationId: fundingCorrelationId,
      idempotencyKey: idempotencyKey,
      status: 'Prepared',
      isReplay: fundingCorrelationIds.length > 1,
      preparedAtUtc: handoff.preparedAtUtc.add(const Duration(seconds: 1)),
    );
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
  MobileMoneyPayoutFundingRepository? fundingRepository,
  MobileMoneyPayoutExecutionRepository? executionRepository,
  String Function()? correlationIdFactory,
  String Function()? idempotencyKeyFactory,
  DateTime Function()? nowUtc,
}) {
  return MaterialApp(
    home: MobileMoneyPayoutQuoteIntentEntryPage(
      sourceWallet: sourceWallet,
      beneficiary: beneficiary,
      quoteRepository: quoteRepository,
      fundingRepository: fundingRepository,
      executionRepository: executionRepository,
      correlationIdFactory: correlationIdFactory,
      idempotencyKeyFactory: idempotencyKeyFactory,
      nowUtc: nowUtc,
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


  testWidgets(
    'wallet selection opens authenticated planning with stable correlation and UTC timestamp across retry',
    (tester) async {
      await _useLargeSurface(tester);
      final quoteRepository = _CapturingQuoteRepository();
      final fundingRepository = _CapturingFundingRepository(failFirst: true);
      final requestedAtUtc = DateTime.utc(2026, 10, 16, 12, 34, 56);
      const correlationId = '11111111-2222-4333-8444-555555555555';

      await tester.pumpWidget(
        _app(
          sourceWallet: _sourceWallet,
          beneficiary: _resolvedBeneficiary,
          quoteRepository: quoteRepository,
          fundingRepository: fundingRepository,
          correlationIdFactory: () => correlationId,
          nowUtc: () => requestedAtUtc,
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

      await tester.tap(find.byKey(const Key('momo-funding-wallet')));
      await tester.pump();
      await tester.scrollUntilVisible(
        find.byKey(const Key('momo-funding-continue')),
        150,
      );
      await tester.tap(find.byKey(const Key('momo-funding-continue')));
      await tester.pumpAndSettle();

      expect(
        find.byType(MobileMoneyPayoutFundingPlanningPage),
        findsOneWidget,
      );
      expect(find.byKey(const Key('momo-funding-plan-failed')), findsOneWidget);
      expect(fundingRepository.correlationIds, <String>[correlationId]);
      expect(
        fundingRepository.requestedAtUtcValues,
        <DateTime>[requestedAtUtc],
      );
      expect(fundingRepository.intents.single.fundingAllocations, hasLength(1));
      expect(
        fundingRepository.intents.single.fundingAllocations.single.sourceId,
        'wallet-eur-primary',
      );

      await tester.tap(find.text('Réessayer'));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('momo-funding-plan-ready')), findsOneWidget);
      expect(
        fundingRepository.correlationIds,
        <String>[correlationId, correlationId],
      );
      expect(
        fundingRepository.requestedAtUtcValues,
        <DateTime>[requestedAtUtc, requestedAtUtc],
      );
      expect(find.byKey(const Key('momo-funding-plan-continue')), findsNothing);

      await tester.tap(find.byKey(const Key('momo-funding-plan-back')));
      await tester.pumpAndSettle();
      expect(
        find.byKey(const Key('momo-funding-selection-page')),
        findsOneWidget,
      );
    },
  );

  testWidgets(
    'expired quote blocks planning before repository call',
    (tester) async {
      await _useLargeSurface(tester);
      final quoteRepository = _CapturingQuoteRepository();
      final fundingRepository = _CapturingFundingRepository();

      await tester.pumpWidget(
        _app(
          sourceWallet: _sourceWallet,
          beneficiary: _resolvedBeneficiary,
          quoteRepository: quoteRepository,
          fundingRepository: fundingRepository,
          correlationIdFactory: () =>
              '11111111-2222-4333-8444-555555555555',
          nowUtc: () => DateTime.utc(2100),
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
      await tester.tap(find.byKey(const Key('momo-funding-wallet')));
      await tester.pump();
      await tester.scrollUntilVisible(
        find.byKey(const Key('momo-funding-continue')),
        150,
      );
      await tester.tap(find.byKey(const Key('momo-funding-continue')));
      await tester.pump();

      expect(fundingRepository.correlationIds, isEmpty);
      expect(
        find.byKey(const Key('momo-funding-plan-expired-snackbar')),
        findsOneWidget,
      );
      expect(
        find.byType(MobileMoneyPayoutFundingPlanningPage),
        findsNothing,
      );
    },
  );

  testWidgets(
    'generated correlation ID is a canonical version-4 GUID and is not derived from user identifiers',
    (tester) async {
      await _useLargeSurface(tester);
      final quoteRepository = _CapturingQuoteRepository();
      final fundingRepository = _CapturingFundingRepository();

      await tester.pumpWidget(
        _app(
          sourceWallet: _sourceWallet,
          beneficiary: _resolvedBeneficiary,
          quoteRepository: quoteRepository,
          fundingRepository: fundingRepository,
          nowUtc: () => DateTime.utc(2026, 10, 16, 12),
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
      await tester.tap(find.byKey(const Key('momo-funding-wallet')));
      await tester.pump();
      await tester.scrollUntilVisible(
        find.byKey(const Key('momo-funding-continue')),
        150,
      );
      await tester.tap(find.byKey(const Key('momo-funding-continue')));
      await tester.pumpAndSettle();

      final correlationId = fundingRepository.correlationIds.single;
      expect(
        correlationId,
        matches(
          RegExp(
            r'^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$',
          ),
        ),
      );
      expect(correlationId.contains('wallet-eur-primary'), isFalse);
      expect(correlationId.contains('237670123456'), isFalse);
      expect(fundingRepository.requestedAtUtcValues.single.isUtc, isTrue);
    },
  );


  testWidgets(
    'confirmed funding plan opens execution preparation with stable opaque idempotency across retry',
    (tester) async {
      await _useLargeSurface(tester);
      final quoteRepository = _CapturingQuoteRepository();
      final fundingRepository = _CapturingFundingRepository();
      final executionRepository = _CapturingExecutionRepository(failFirst: true);
      const correlationId = '11111111-2222-4333-8444-555555555555';
      final transitionAtUtc = DateTime.utc(2026, 10, 16, 12, 34, 56);

      await tester.pumpWidget(
        _app(
          sourceWallet: _sourceWallet,
          beneficiary: _resolvedBeneficiary,
          quoteRepository: quoteRepository,
          fundingRepository: fundingRepository,
          executionRepository: executionRepository,
          correlationIdFactory: () => correlationId,
          nowUtc: () => transitionAtUtc,
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
      await tester.tap(find.byKey(const Key('momo-funding-wallet')));
      await tester.pump();
      await tester.scrollUntilVisible(
        find.byKey(const Key('momo-funding-continue')),
        150,
      );
      await tester.tap(find.byKey(const Key('momo-funding-continue')));
      await tester.pumpAndSettle();
      await tester.scrollUntilVisible(
        find.byKey(const Key('momo-funding-plan-continue')),
        150,
      );
      await tester.tap(find.byKey(const Key('momo-funding-plan-continue')));
      await tester.pumpAndSettle();

      expect(
        find.byType(MobileMoneyPayoutExecutionPreparationPage),
        findsOneWidget,
      );
      expect(
        find.byKey(const Key('momo-execution-preparation-failed')),
        findsOneWidget,
      );
      expect(executionRepository.fundingCorrelationIds, <String>[correlationId]);
      expect(executionRepository.idempotencyKeys, hasLength(1));
      final generatedKey = executionRepository.idempotencyKeys.single;
      expect(
        generatedKey,
        matches(
          RegExp(
            r'^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$',
          ),
        ),
      );
      expect(generatedKey.contains('wallet-eur-primary'), isFalse);
      expect(generatedKey.contains('237670123456'), isFalse);
      expect(executionRepository.handoffs.single.quoteId, 'quote-entry');
      expect(
        executionRepository.handoffs.single.fundingAllocations.single.sourceId,
        'wallet-eur-primary',
      );

      await tester.tap(
        find.byKey(const Key('momo-execution-preparation-retry')),
      );
      await tester.pumpAndSettle();

      expect(
        find.byKey(const Key('momo-execution-preparation-ready')),
        findsOneWidget,
      );
      expect(
        executionRepository.fundingCorrelationIds,
        <String>[correlationId, correlationId],
      );
      expect(
        executionRepository.idempotencyKeys,
        <String>[generatedKey, generatedKey],
      );
      expect(
        identical(
          executionRepository.handoffs[0],
          executionRepository.handoffs[1],
        ),
        isTrue,
      );
      expect(
        find.byKey(const Key('momo-execution-preparation-continue')),
        findsNothing,
      );

      await tester.tap(
        find.byKey(const Key('momo-execution-preparation-back')),
      );
      await tester.pumpAndSettle();
      expect(find.byKey(const Key('momo-funding-plan-ready')), findsOneWidget);
    },
  );

  testWidgets(
    'expired quote blocks execution preparation before repository call',
    (tester) async {
      await _useLargeSurface(tester);
      final quoteRepository = _CapturingQuoteRepository();
      final fundingRepository = _CapturingFundingRepository();
      final executionRepository = _CapturingExecutionRepository();
      var nowCalls = 0;

      await tester.pumpWidget(
        _app(
          sourceWallet: _sourceWallet,
          beneficiary: _resolvedBeneficiary,
          quoteRepository: quoteRepository,
          fundingRepository: fundingRepository,
          executionRepository: executionRepository,
          correlationIdFactory: () =>
              '11111111-2222-4333-8444-555555555555',
          idempotencyKeyFactory: () =>
              '44444444-4444-4444-8444-444444444444',
          nowUtc: () {
            nowCalls += 1;
            return nowCalls == 1
                ? DateTime.utc(2026, 10, 16, 12)
                : DateTime.utc(2100);
          },
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
      await tester.tap(find.byKey(const Key('momo-funding-wallet')));
      await tester.pump();
      await tester.scrollUntilVisible(
        find.byKey(const Key('momo-funding-continue')),
        150,
      );
      await tester.tap(find.byKey(const Key('momo-funding-continue')));
      await tester.pumpAndSettle();
      await tester.scrollUntilVisible(
        find.byKey(const Key('momo-funding-plan-continue')),
        150,
      );
      await tester.tap(find.byKey(const Key('momo-funding-plan-continue')));
      await tester.pump();

      expect(executionRepository.fundingCorrelationIds, isEmpty);
      expect(
        find.byKey(const Key('momo-execution-preparation-invalid-snackbar')),
        findsOneWidget,
      );
      expect(
        find.byType(MobileMoneyPayoutExecutionPreparationPage),
        findsNothing,
      );
    },
  );

}

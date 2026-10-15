import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/mobile_money_beneficiary_lookup.dart';
import 'package:mobile_app/models/mobile_money_beneficiary_operator.dart';
import 'package:mobile_app/models/transaction_history.dart';
import 'package:mobile_app/models/wallet_balance.dart';
import 'package:mobile_app/pages/wallet_home_page.dart';
import 'package:mobile_app/services/mobile_money_beneficiary_lookup_repository.dart';
import 'package:mobile_app/services/transaction_history_repository.dart';
import 'package:mobile_app/services/wallet_repository.dart';

class _ReadyWalletRepository implements WalletRepository {
  @override
  Future<List<WalletBalance>> loadWalletBalances() async {
    return const [
      WalletBalance(
        walletId: 'WALLET-TEST-EUR',
        currency: 'EUR',
        availableMinor: 12345,
        status: 'ACTIVE',
        countryCode: 'DE',
      ),
      WalletBalance(
        walletId: 'WALLET-TEST-XAF',
        currency: 'XAF',
        availableMinor: 500000,
        status: 'ACTIVE',
        countryCode: 'CM',
      ),
    ];
  }
}


class _ReadyMobileMoneyBeneficiaryLookupRepository
    implements MobileMoneyBeneficiaryLookupRepository {
  @override
  Future<MobileMoneyBeneficiarySearchResult> search({
    required String phoneNumber,
  }) async {
    return const MobileMoneyBeneficiarySearchResult(
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
  }
}

class _UnavailableWalletRepository implements WalletRepository {
  @override
  Future<List<WalletBalance>> loadWalletBalances() async {
    throw const WalletUnavailableException();
  }
}

class _ReadyTimelineRepository implements TransactionHistoryRepository {
  @override
  Future<List<TransactionHistoryItem>> listTransactions() async => [
        TransactionHistoryItem(
          transactionId: 'TX-OLDER',
          amountMinor: 500,
          currencyCode: 'EUR',
          direction: TransactionDirection.outgoing,
          status: TransactionHistoryStatus.completed,
          occurredAt: DateTime.utc(2026, 8, 31, 8),
          reference: 'OLDER',
          counterpartyLabel: '@older',
        ),
        TransactionHistoryItem(
          transactionId: 'TX-LATEST',
          amountMinor: 1250,
          currencyCode: 'EUR',
          direction: TransactionDirection.incoming,
          status: TransactionHistoryStatus.completed,
          occurredAt: DateTime.utc(2026, 9, 1, 8),
          reference: 'LATEST',
          counterpartyLabel: '@latest',
        ),
      ];
}

void main() {
  testWidgets('renders balances supplied by wallet repository', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(MaterialApp(
      home: WalletHomePage(
        repository: _ReadyWalletRepository(),
        transactionHistoryRepository: _ReadyTimelineRepository(),
        afWalId: '@afwal-test',
      ),
    ));

    await tester.pumpAndSettle();

    expect(find.text('Wallet Home'), findsOneWidget);
    expect(find.byKey(const Key('wallet-home-brand')), findsOneWidget);
    expect(find.text('AFRIKAWALLET'), findsOneWidget);
    expect(find.byKey(const Key('wallet-home-greeting')), findsOneWidget);
    expect(find.text('Solde disponible'), findsOneWidget);
    expect(find.byKey(const Key('wallet-home-primary-balance')), findsOneWidget);
    expect(find.byKey(const Key('wallet-home-primary-balance-amount')), findsOneWidget);
    expect(find.byKey(const Key('wallet-home-hero-balance-state')), findsOneWidget);
    expect(find.byKey(const Key('wallet-home-afwal-id')), findsNothing);
    expect(find.text('@afwal-test'), findsNothing);
    expect(find.text('Mes wallets'), findsOneWidget);
    expect(find.text('123.45 EUR'), findsNWidgets(2));
    expect(find.text('5000.00 XAF'), findsOneWidget);
    expect(find.text('Wallet WALLET-TEST-EUR'), findsOneWidget);
    expect(find.byKey(const Key('wallet-quick-actions')), findsOneWidget);
    expect(find.byKey(const Key('wallet-quick-action-send')), findsOneWidget);
    expect(find.text('Envoyer'), findsOneWidget);
    expect(find.byKey(const Key('wallet-quick-action-receive')), findsOneWidget);
    expect(find.text('Recevoir'), findsOneWidget);
    expect(
      find.byKey(const Key('wallet-quick-action-mobile-money')),
      findsOneWidget,
    );
    expect(find.text('Mobile Money'), findsOneWidget);
    expect(find.byKey(const Key('wallet-quick-action-qr')), findsOneWidget);
    expect(find.text('QR'), findsOneWidget);
    expect(
      find.byKey(const Key('wallet-quick-action-subscriptions')),
      findsOneWidget,
    );
    expect(find.byKey(const Key('wallet-subscriptions-action')), findsOneWidget);
    expect(find.text('Abonnements'), findsOneWidget);
    expect(find.text('Financial Timeline'), findsOneWidget);
  });


  testWidgets('exposes stable semantics for the four quick actions', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(MaterialApp(
      home: WalletHomePage(
        repository: _ReadyWalletRepository(),
        transactionHistoryRepository: _ReadyTimelineRepository(),
        onSend: () {},
        onReceive: () {},
        onQr: () {},
        onSubscriptions: () {},
      ),
    ));
    await tester.pumpAndSettle();

    final expectations = <Key, (String, String)>{
      const Key('wallet-quick-action-send'): (
        'Envoyer de l’argent',
        'Ouvrir l’envoi depuis votre portefeuille',
      ),
      const Key('wallet-quick-action-receive'): (
        'Recevoir de l’argent',
        'Afficher les options pour recevoir',
      ),
      const Key('wallet-quick-action-qr'): (
        'Paiements QR',
        'Ouvrir les paiements QR',
      ),
      const Key('wallet-quick-action-subscriptions'): (
        'Gérer mes abonnements',
        'Ouvrir les abonnements AfrikaWallet',
      ),
    };

    for (final entry in expectations.entries) {
      final semantics = tester.widget<Semantics>(find.byKey(entry.key));
      expect(semantics.properties.button, isTrue);
      expect(semantics.properties.label, entry.value.$1);
      expect(semantics.properties.hint, entry.value.$2);
      expect(semantics.properties.onTap, isNotNull);
    }
  });

  testWidgets(
    'transitions between balance and authoritative AfWal ID hero states',
    (tester) async {
      await tester.binding.setSurfaceSize(const Size(900, 1400));
      addTearDown(() => tester.binding.setSurfaceSize(null));

      await tester.pumpWidget(MaterialApp(
        home: WalletHomePage(
          repository: _ReadyWalletRepository(),
          transactionHistoryRepository: _ReadyTimelineRepository(),
          afWalId: '@afwal-test',
        ),
      ));
      await tester.pumpAndSettle();

      expect(
        find.byKey(const Key('wallet-home-hero-balance-state')),
        findsOneWidget,
      );
      expect(
        find.byKey(const Key('wallet-home-primary-balance-amount')),
        findsOneWidget,
      );
      expect(find.byKey(const Key('wallet-home-afwal-id')), findsNothing);

      await tester.tap(find.byKey(const Key('wallet-home-primary-balance')));
      await tester.pumpAndSettle();

      expect(
        find.byKey(const Key('wallet-home-hero-afwal-id-state')),
        findsOneWidget,
      );
      expect(find.byKey(const Key('wallet-home-afwal-id')), findsOneWidget);
      expect(find.text('@afwal-test'), findsOneWidget);
      expect(
        find.byKey(const Key('wallet-home-primary-balance-amount')),
        findsNothing,
      );

      await tester.tap(find.byKey(const Key('wallet-home-primary-balance')));
      await tester.pumpAndSettle();

      expect(
        find.byKey(const Key('wallet-home-hero-balance-state')),
        findsOneWidget,
      );
      expect(
        find.byKey(const Key('wallet-home-primary-balance-amount')),
        findsOneWidget,
      );
      expect(find.byKey(const Key('wallet-home-afwal-id')), findsNothing);
    },
  );

  testWidgets('does not fabricate AfWal ID when none is supplied', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(MaterialApp(
      home: WalletHomePage(
        repository: _ReadyWalletRepository(),
        transactionHistoryRepository: _ReadyTimelineRepository(),
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('wallet-home-primary-balance')), findsOneWidget);
    expect(find.text('Solde disponible'), findsOneWidget);
    expect(find.text('AfWal ID'), findsNothing);
    expect(find.byKey(const Key('wallet-home-afwal-id')), findsNothing);

    await tester.tap(find.byKey(const Key('wallet-home-primary-balance')));
    await tester.pumpAndSettle();

    expect(find.text('Solde disponible'), findsOneWidget);
    expect(
      find.byKey(const Key('wallet-home-primary-balance-amount')),
      findsOneWidget,
    );
    expect(find.byKey(const Key('wallet-home-afwal-id')), findsNothing);
  });

  testWidgets('routes all four quick actions through distinct callbacks', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    var sendCount = 0;
    var receiveCount = 0;
    var qrCount = 0;
    var subscriptionsCount = 0;

    await tester.pumpWidget(MaterialApp(
      home: WalletHomePage(
        repository: _ReadyWalletRepository(),
        transactionHistoryRepository: _ReadyTimelineRepository(),
        onSend: () => sendCount += 1,
        onReceive: () => receiveCount += 1,
        onQr: () => qrCount += 1,
        onSubscriptions: () => subscriptionsCount += 1,
      ),
    ));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('wallet-quick-action-send')));
    expect(sendCount, 1);
    expect(receiveCount, 0);
    expect(qrCount, 0);
    expect(subscriptionsCount, 0);

    await tester.tap(find.byKey(const Key('wallet-quick-action-receive')));
    expect(sendCount, 1);
    expect(receiveCount, 1);
    expect(qrCount, 0);
    expect(subscriptionsCount, 0);

    await tester.tap(find.byKey(const Key('wallet-quick-action-qr')));
    expect(sendCount, 1);
    expect(receiveCount, 1);
    expect(qrCount, 1);
    expect(subscriptionsCount, 0);

    await tester.tap(
      find.byKey(const Key('wallet-quick-action-subscriptions')),
    );
    expect(sendCount, 1);
    expect(receiveCount, 1);
    expect(qrCount, 1);
    expect(subscriptionsCount, 1);
  });


  testWidgets(
    'opens Mobile Money beneficiary search and returns to Wallet Home',
    (tester) async {
      await tester.binding.setSurfaceSize(const Size(900, 1400));
      addTearDown(() => tester.binding.setSurfaceSize(null));

      await tester.pumpWidget(MaterialApp(
        home: WalletHomePage(
          repository: _ReadyWalletRepository(),
          transactionHistoryRepository: _ReadyTimelineRepository(),
          mobileMoneyBeneficiaryLookupRepository:
              _ReadyMobileMoneyBeneficiaryLookupRepository(),
        ),
      ));
      await tester.pumpAndSettle();

      final mobileMoneyAction =
          find.byKey(const Key('wallet-quick-action-mobile-money'));
      expect(mobileMoneyAction, findsOneWidget);
      final semantics = tester.widget<Semantics>(mobileMoneyAction);
      expect(semantics.properties.button, isTrue);
      expect(semantics.properties.label, 'Mobile Money');
      expect(
        semantics.properties.hint,
        'Rechercher un bénéficiaire Mobile Money',
      );
      expect(semantics.properties.onTap, isNotNull);

      await tester.ensureVisible(mobileMoneyAction);
      await tester.tap(mobileMoneyAction);
      await tester.pumpAndSettle();

      expect(find.text('Ajouter un bénéficiaire'), findsOneWidget);
      expect(find.text('Rechercher un bénéficiaire Mobile Money'), findsOneWidget);
      expect(
        find.byKey(const Key('momo-beneficiary-search-back')),
        findsOneWidget,
      );
      expect(find.text('Wallet Home'), findsNothing);

      await tester.tap(find.byKey(const Key('momo-beneficiary-search-back')));
      await tester.pumpAndSettle();

      expect(find.text('Mon portefeuille'), findsOneWidget);
      expect(find.byKey(const Key('wallet-quick-actions')), findsOneWidget);
      expect(find.text('Ajouter un bénéficiaire'), findsNothing);
    },
  );

  testWidgets('opens QR Payments and returns to Wallet Home', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(MaterialApp(
      home: WalletHomePage(
        repository: _ReadyWalletRepository(),
        transactionHistoryRepository: _ReadyTimelineRepository(),
      ),
    ));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('wallet-quick-action-qr')));
    await tester.pumpAndSettle();

    expect(find.text('QR Payments'), findsOneWidget);
    expect(find.byKey(const Key('qr-return-to-wallet')), findsOneWidget);
    expect(find.text('Wallet Home'), findsNothing);

    await tester.tap(find.byKey(const Key('qr-return-to-wallet')));
    await tester.pumpAndSettle();

    expect(find.text('Wallet Home'), findsOneWidget);
    expect(find.text('QR Payments'), findsNothing);
  });

  testWidgets('renders recent financial activity from repository', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(MaterialApp(
      home: WalletHomePage(
        repository: _ReadyWalletRepository(),
        transactionHistoryRepository: _ReadyTimelineRepository(),
      ),
    ));

    await tester.pumpAndSettle();

    expect(find.text('@latest'), findsOneWidget);
    expect(find.text('+12.50 EUR'), findsOneWidget);
    expect(find.text('@older'), findsOneWidget);
    expect(find.text('-5.00 EUR'), findsOneWidget);
    expect(find.text('Voir tout'), findsOneWidget);

    final latestY = tester.getTopLeft(find.text('@latest')).dy;
    final olderY = tester.getTopLeft(find.text('@older')).dy;
    expect(latestY, lessThan(olderY));
  });

  testWidgets('timeline never fabricates transactions when unavailable', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(MaterialApp(
      home: WalletHomePage(
        repository: _ReadyWalletRepository(),
        transactionHistoryRepository: const UnavailableTransactionHistoryRepository(),
      ),
    ));

    await tester.pumpAndSettle();

    expect(find.text('Timeline indisponible'), findsOneWidget);
    expect(find.textContaining('Aucune transaction n’est simulée'), findsOneWidget);
    expect(find.text('Réessayer'), findsOneWidget);
  });

  testWidgets('never fabricates balances when repository is unavailable', (tester) async {
    await tester.pumpWidget(MaterialApp(
      home: WalletHomePage(repository: _UnavailableWalletRepository()),
    ));

    await tester.pumpAndSettle();

    expect(find.text('Soldes indisponibles'), findsOneWidget);
    expect(find.textContaining('Aucun solde n’est simulé'), findsOneWidget);
    expect(find.text('Réessayer'), findsOneWidget);
  });

  test('formats repository minor units without floating point mutation', () {
    const balance = WalletBalance(
      walletId: 'W1',
      currency: 'EUR',
      availableMinor: -105,
      status: 'ACTIVE',
    );
    expect(balance.formattedAmount, '-1.05 EUR');
  });
}
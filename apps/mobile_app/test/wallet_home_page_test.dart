import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/transaction_history.dart';
import 'package:mobile_app/models/wallet_balance.dart';
import 'package:mobile_app/pages/wallet_home_page.dart';
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
      ),
    ));

    await tester.pumpAndSettle();

    expect(find.text('Wallet Home'), findsOneWidget);
    expect(find.text('123.45 EUR'), findsOneWidget);
    expect(find.text('5000.00 XAF'), findsOneWidget);
    expect(find.text('Wallet WALLET-TEST-EUR'), findsOneWidget);
    expect(find.text('Envoyer'), findsOneWidget);
    expect(find.text('Recevoir'), findsOneWidget);
    expect(find.byKey(const Key('wallet-quick-action-qr')), findsOneWidget);
    expect(find.text('QR'), findsOneWidget);
    expect(find.text('Financial Timeline'), findsOneWidget);
  });

  testWidgets('routes send receive and QR through distinct callbacks', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    var sendCount = 0;
    var receiveCount = 0;
    var qrCount = 0;

    await tester.pumpWidget(MaterialApp(
      home: WalletHomePage(
        repository: _ReadyWalletRepository(),
        transactionHistoryRepository: _ReadyTimelineRepository(),
        onSend: () => sendCount += 1,
        onReceive: () => receiveCount += 1,
        onQr: () => qrCount += 1,
      ),
    ));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Envoyer'));
    expect(sendCount, 1);
    expect(receiveCount, 0);
    expect(qrCount, 0);

    await tester.tap(find.text('Recevoir'));
    expect(sendCount, 1);
    expect(receiveCount, 1);
    expect(qrCount, 0);

    await tester.tap(find.byKey(const Key('wallet-quick-action-qr')));
    expect(sendCount, 1);
    expect(receiveCount, 1);
    expect(qrCount, 1);
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

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/transaction_history.dart';
import 'package:mobile_app/pages/transaction_history_page.dart';
import 'package:mobile_app/services/transaction_history_repository.dart';

class FakeTransactionHistoryRepository implements TransactionHistoryRepository {
  @override
  Future<List<TransactionHistoryItem>> listTransactions() async => [
        TransactionHistoryItem(
          transactionId: 'TX-001',
          amountMinor: 1250,
          currencyCode: 'EUR',
          direction: TransactionDirection.outgoing,
          status: TransactionHistoryStatus.completed,
          occurredAt: DateTime.utc(2026, 9, 1, 8),
          reference: 'PI-001',
          counterpartyLabel: '@receiver',
        ),
      ];
}

class EmptyTransactionHistoryRepository implements TransactionHistoryRepository {
  @override
  Future<List<TransactionHistoryItem>> listTransactions() async => const [];
}

void main() {
  testWidgets('renders repository transaction and details', (tester) async {
    await tester.pumpWidget(MaterialApp(
      home: TransactionHistoryPage(
        repository: FakeTransactionHistoryRepository(),
        onReturnToWallet: () {},
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.text('Financial Timeline'), findsOneWidget);
    expect(find.text('@receiver'), findsOneWidget);
    expect(find.text('-12.50 EUR'), findsOneWidget);
    expect(find.text('Terminée'), findsOneWidget);

    await tester.tap(find.text('@receiver'));
    await tester.pumpAndSettle();
    expect(find.text('Détail de la transaction'), findsOneWidget);
    expect(find.text('Référence : PI-001'), findsOneWidget);
    expect(find.text('ID : TX-001'), findsOneWidget);
  });

  testWidgets('returns to wallet from populated Financial Timeline', (tester) async {
    var returnCount = 0;
    await tester.pumpWidget(MaterialApp(
      home: TransactionHistoryPage(
        repository: FakeTransactionHistoryRepository(),
        onReturnToWallet: () => returnCount++,
      ),
    ));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('return-to-wallet-history-list')));
    await tester.pump();

    expect(returnCount, 1);
  });

  testWidgets('returns to wallet from empty Financial Timeline', (tester) async {
    var returnCount = 0;
    await tester.pumpWidget(MaterialApp(
      home: TransactionHistoryPage(
        repository: EmptyTransactionHistoryRepository(),
        onReturnToWallet: () => returnCount++,
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.text('Aucune transaction'), findsOneWidget);
    await tester.tap(find.byKey(const Key('return-to-wallet-history-empty')));
    await tester.pump();

    expect(returnCount, 1);
  });

  testWidgets('unavailable repository never fabricates history and can return to wallet', (tester) async {
    var returnCount = 0;
    await tester.pumpWidget(MaterialApp(
      home: TransactionHistoryPage(
        repository: const UnavailableTransactionHistoryRepository(),
        onReturnToWallet: () => returnCount++,
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.text('Historique indisponible'), findsOneWidget);
    expect(find.textContaining('Aucune transaction n’est simulée'), findsOneWidget);

    await tester.tap(find.byKey(const Key('return-to-wallet-history-error')));
    await tester.pump();

    expect(returnCount, 1);
  });

  testWidgets('preserves optional legacy continuation', (tester) async {
    var continueCount = 0;
    await tester.pumpWidget(MaterialApp(
      home: TransactionHistoryPage(
        repository: FakeTransactionHistoryRepository(),
        onReturnToWallet: () {},
        onContinue: () => continueCount++,
      ),
    ));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Continuer'));
    await tester.pump();

    expect(continueCount, 1);
  });
}

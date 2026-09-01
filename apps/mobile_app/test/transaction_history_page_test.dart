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

void main() {
  testWidgets('renders repository transaction and details', (tester) async {
    await tester.pumpWidget(MaterialApp(
      home: TransactionHistoryPage(repository: FakeTransactionHistoryRepository(), onContinue: () {}),
    ));
    await tester.pumpAndSettle();

    expect(find.text('@receiver'), findsOneWidget);
    expect(find.text('-12.50 EUR'), findsOneWidget);
    expect(find.text('Terminée'), findsOneWidget);

    await tester.tap(find.text('@receiver'));
    await tester.pumpAndSettle();
    expect(find.text('Détail de la transaction'), findsOneWidget);
    expect(find.text('Référence : PI-001'), findsOneWidget);
    expect(find.text('ID : TX-001'), findsOneWidget);
  });

  testWidgets('unavailable repository never fabricates history', (tester) async {
    await tester.pumpWidget(MaterialApp(
      home: TransactionHistoryPage(
        repository: const UnavailableTransactionHistoryRepository(),
        onContinue: () {},
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.text('Historique indisponible'), findsOneWidget);
    expect(find.textContaining('Aucune transaction n’est simulée'), findsOneWidget);
  });
}

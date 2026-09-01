import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/wallet_balance.dart';
import 'package:mobile_app/pages/wallet_home_page.dart';
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

void main() {
  testWidgets('renders balances supplied by wallet repository', (tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 1200));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(MaterialApp(
      home: WalletHomePage(repository: _ReadyWalletRepository()),
    ));

    await tester.pumpAndSettle();

    expect(find.text('Wallet Home'), findsOneWidget);
    expect(find.text('123.45 EUR'), findsOneWidget);
    expect(find.text('5000.00 XAF'), findsOneWidget);
    expect(find.text('Wallet WALLET-TEST-EUR'), findsOneWidget);
    expect(find.text('Envoyer bientôt'), findsOneWidget);
    expect(find.text('Recevoir bientôt'), findsOneWidget);
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

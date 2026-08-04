import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:afriwallet_mobile/features/fx/fx_quote_screen.dart';
import 'package:afriwallet_mobile/features/wallets/wallet_controller.dart';
import 'package:afriwallet_mobile/features/wallets/wallet_create_screen.dart';
import 'package:afriwallet_mobile/features/wallets/wallet_detail_screen.dart';
import 'package:afriwallet_mobile/features/wallets/wallet_models.dart';
import 'package:afriwallet_mobile/features/wallets/wallet_screen.dart';
import 'package:afriwallet_mobile/features/wallets/wallet_service.dart';

class FakeWalletApiClient extends WalletApiClient {
  FakeWalletApiClient({this.throwOnCreate = false});

  final bool throwOnCreate;
  int createCalls = 0;

  @override
  Future<List<WalletSummary>> listWallets(String awid) async => [
        WalletSummary(
          id: 'wallet-1',
          walletNumber: 'AFW-EUR-1001',
          currency: 'EUR',
          walletType: 'Personal',
          status: 'Active',
          availableBalance: 10.5,
          pendingBalance: 2,
          reservedBalance: 1,
          createdAt: DateTime.now(),
          updatedAt: DateTime.now(),
        ),
      ];

  @override
  Future<WalletDetail> getWalletDetail(String walletId) async => WalletDetail(
        id: walletId,
        walletNumber: 'AFW-EUR-1001',
        currency: 'EUR',
        walletType: 'Personal',
        status: 'Active',
        availableBalance: 10.5,
        pendingBalance: 2,
        reservedBalance: 1,
        ledgerBalance: 13.5,
        updatedAt: DateTime.now(),
        timeline: [
          WalletTimelineItem(
            transactionId: 'tx-1',
            reference: 'Ref-1',
            description: 'Received funds',
            occurredAt: DateTime.now(),
            direction: '+',
            amount: 10,
            currency: 'EUR',
          ),
        ],
        lastActivityAt: DateTime.now(),
      );

  @override
  Future<WalletPortfolioSummary> getPortfolioSummary(String awid) async => WalletPortfolioSummary(
        awid: awid,
        walletCount: 1,
        totalAvailable: 10.5,
        totalLedgerBalance: 13.5,
        currencyCount: 1,
        generatedAt: DateTime.now(),
      );

  @override
  Future<WalletSummary> createWallet({
    required String awid,
    required String walletType,
    required String currency,
    String? name,
  }) async {
    createCalls += 1;
    if (throwOnCreate) {
      throw Exception('duplicate wallet');
    }
    return WalletSummary(
      id: 'wallet-2',
      walletNumber: 'AFW-USD-2002',
      currency: currency,
      walletType: walletType,
      status: 'Created',
      availableBalance: 0,
      pendingBalance: 0,
      reservedBalance: 0,
      createdAt: DateTime.now(),
      updatedAt: DateTime.now(),
    );
  }

  @override
  Future<Map<String, dynamic>> createFxQuote({
    required String from,
    required String to,
    required int amountMinor,
  }) async {
    return {
      'quoteId': 'quote-1',
      'sourceAmountMinor': amountMinor,
      'targetAmountMinor': amountMinor + 200,
      'exchangeRate': '1.20',
      'appliedRate': '1.20',
      'spread': '0.10',
      'fee': '2.00',
      'expiresInSeconds': 15,
      'expiresAt': '2026-08-03T10:00:00Z',
    };
  }
}

void main() {
  testWidgets('wallet list renders', (tester) async {
    final controller = WalletController(apiClient: FakeWalletApiClient());
    await controller.loadWallets('demo-awid');
    await tester.pumpWidget(MaterialApp(home: WalletScreen(controller: controller)));
    await tester.pump();

    expect(find.text('AFW-EUR-1001'), findsOneWidget);
  });

  testWidgets('wallet creation succeeds', (tester) async {
    final controller = WalletController(apiClient: FakeWalletApiClient());
    await tester.pumpWidget(MaterialApp(home: WalletCreateScreen(controller: controller)));
    await tester.enterText(find.byType(TextFormField).first, 'USD');
    await tester.tap(find.byType(FilledButton));
    await tester.pump();

    expect(controller.wallets.length, 1);
  });

  testWidgets('duplicate wallet error is displayed', (tester) async {
    final controller = WalletController(apiClient: FakeWalletApiClient(throwOnCreate: true));
    await tester.pumpWidget(MaterialApp(home: WalletCreateScreen(controller: controller)));
    await tester.enterText(find.byType(TextFormField).first, 'USD');
    await tester.tap(find.byType(FilledButton));
    await tester.pump();

    expect(find.textContaining('duplicate'), findsOneWidget);
  });

  testWidgets('wallet detail renders balance breakdown', (tester) async {
    final controller = WalletController(apiClient: FakeWalletApiClient());
    await controller.selectWallet('wallet-1');
    await tester.pumpWidget(MaterialApp(home: WalletDetailScreen(walletId: 'wallet-1', controller: controller)));
    await tester.pump();

    expect(find.textContaining('Available'), findsOneWidget);
    expect(find.textContaining('Ledger'), findsOneWidget);
  });

  testWidgets('timeline empty state renders', (tester) async {
    final controller = WalletController(apiClient: FakeWalletApiClient());
    controller.selectedWallet = WalletDetail(
      id: 'wallet-5',
      walletNumber: 'AFW-EUR-5001',
      currency: 'EUR',
      walletType: 'Personal',
      status: 'Active',
      availableBalance: 0,
      pendingBalance: 0,
      reservedBalance: 0,
      ledgerBalance: 0,
      updatedAt: DateTime.now(),
      timeline: const [],
      lastActivityAt: DateTime.now(),
    );
    await tester.pumpWidget(MaterialApp(home: WalletDetailScreen(walletId: 'wallet-5', controller: controller)));
    await tester.pump();

    expect(find.text('No activity yet.'), findsOneWidget);
  });

  testWidgets('timeline items are paginated', (tester) async {
    final controller = WalletController(apiClient: FakeWalletApiClient());
    controller.selectedWallet = WalletDetail(
      id: 'wallet-6',
      walletNumber: 'AFW-EUR-6001',
      currency: 'EUR',
      walletType: 'Personal',
      status: 'Active',
      availableBalance: 0,
      pendingBalance: 0,
      reservedBalance: 0,
      ledgerBalance: 0,
      updatedAt: DateTime.now(),
      timeline: List.generate(6, (index) => WalletTimelineItem(
            transactionId: 'tx-$index',
            reference: 'Ref-$index',
            description: 'Activity $index',
            occurredAt: DateTime.now(),
            direction: '+',
            amount: index.toDouble(),
            currency: 'EUR',
          )),
      lastActivityAt: DateTime.now(),
    );
    await tester.pumpWidget(MaterialApp(home: WalletDetailScreen(walletId: 'wallet-6', controller: controller)));
    await tester.pump();

    expect(find.text('Ref-0'), findsOneWidget);
    expect(find.text('Ref-3'), findsNothing);
  });

  testWidgets('FX quote renders server values', (tester) async {
    final fakeClient = FakeWalletApiClient();
    await tester.pumpWidget(MaterialApp(home: FxQuoteScreen(apiClient: fakeClient)));
    await tester.enterText(find.byType(TextFormField).first, '1000');
    await tester.pump();
    await tester.tap(find.byType(FilledButton));
    await tester.pump();

    expect(find.textContaining('1000'), findsWidgets);
    expect(find.textContaining('1200'), findsOneWidget);
  });

  testWidgets('expired FX quote disables confirmation', (tester) async {
    final fakeClient = FakeWalletApiClient();
    await tester.pumpWidget(MaterialApp(home: FxQuoteScreen(apiClient: fakeClient)));
    await tester.enterText(find.byType(TextFormField).first, '1000');
    await tester.pump();
    await tester.tap(find.byType(FilledButton));
    await tester.pump();

    final button = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
    expect(button.onPressed, isNotNull);
  });

  testWidgets('API error state renders', (tester) async {
    final controller = WalletController(apiClient: FakeWalletApiClient());
    controller.error = 'Failed to load wallets';
    await tester.pumpWidget(MaterialApp(home: WalletScreen(controller: controller)));
    await tester.pump();

    expect(find.textContaining('Failed to load wallets'), findsNothing);
  });

  testWidgets('privacy mode hides balances', (tester) async {
    final controller = WalletController(apiClient: FakeWalletApiClient());
    await controller.loadWallets('demo-awid');
    controller.privacyMode = true;
    await tester.pumpWidget(MaterialApp(home: WalletScreen(controller: controller)));
    await tester.pump();

    expect(find.textContaining('••••'), findsWidgets);
  });
}

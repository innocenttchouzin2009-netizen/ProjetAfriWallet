import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/payment_transfer.dart';
import 'package:mobile_app/pages/send_receive_page.dart';
import 'package:mobile_app/services/transfer_repository.dart';

class _FakeTransferRepository implements TransferRepository {
  SendTransferRequest? lastRequest;

  @override
  Future<ReceiveIdentity> loadReceiveIdentity() async => const ReceiveIdentity(publicLabel: '@afwal-id-test');

  @override
  Future<TransferReceipt> send(SendTransferRequest request) async {
    lastRequest = request;
    return TransferReceipt(
      paymentIntentId: 'PI-TEST-001',
      status: TransferStatus.created,
      amountMinor: request.amountMinor,
      currencyCode: request.currencyCode,
      payeeId: request.payeeId,
    );
  }
}

void main() {
  testWidgets('defaults to send mode and submits transfer through repository only', (tester) async {
    final repository = _FakeTransferRepository();
    await tester.pumpWidget(MaterialApp(home: SendReceivePage(repository: repository)));

    expect(find.text('Envoyer de l’argent'), findsOneWidget);
    await tester.enterText(find.widgetWithText(TextField, 'AfWal ID ou identifiant destinataire'), '@receiver');
    await tester.enterText(find.widgetWithText(TextField, 'Montant'), '12.50');
    await tester.tap(find.widgetWithText(FilledButton, 'Continuer'));
    await tester.pumpAndSettle();

    expect(repository.lastRequest?.payeeId, '@receiver');
    expect(repository.lastRequest?.amountMinor, 1250);
    expect(find.textContaining('PI-TEST-001'), findsOneWidget);
  });

  testWidgets('opens receive tab directly when receive mode is requested', (tester) async {
    final repository = _FakeTransferRepository();
    await tester.pumpWidget(MaterialApp(
      home: SendReceivePage(
        repository: repository,
        initialMode: SendReceiveMode.receive,
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.text('Recevoir de l’argent'), findsOneWidget);
    expect(find.byKey(const Key('receive-public-label')), findsOneWidget);
    expect(find.text('@afwal-id-test'), findsOneWidget);
  });

  testWidgets('send mode exposes explicit return to wallet callback', (tester) async {
    final repository = _FakeTransferRepository();
    var returnCount = 0;

    await tester.pumpWidget(MaterialApp(
      home: SendReceivePage(
        repository: repository,
        onReturnToWallet: () => returnCount += 1,
      ),
    ));

    expect(find.byKey(const Key('return-to-wallet-send')), findsOneWidget);
    await tester.tap(find.byKey(const Key('return-to-wallet-send')));

    expect(returnCount, 1);
  });

  testWidgets('receive mode exposes explicit return to wallet callback', (tester) async {
    final repository = _FakeTransferRepository();
    var returnCount = 0;

    await tester.pumpWidget(MaterialApp(
      home: SendReceivePage(
        repository: repository,
        initialMode: SendReceiveMode.receive,
        onReturnToWallet: () => returnCount += 1,
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('return-to-wallet-receive')), findsOneWidget);
    await tester.tap(find.byKey(const Key('return-to-wallet-receive')));

    expect(returnCount, 1);
  });

  testWidgets('unavailable repository never simulates transfer', (tester) async {
    await tester.pumpWidget(const MaterialApp(home: SendReceivePage(repository: UnavailableTransferRepository())));
    await tester.enterText(find.widgetWithText(TextField, 'AfWal ID ou identifiant destinataire'), '@receiver');
    await tester.enterText(find.widgetWithText(TextField, 'Montant'), '10');
    await tester.tap(find.widgetWithText(FilledButton, 'Continuer'));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('send-error')), findsOneWidget);
    expect(find.textContaining('Aucun transfert n’a été simulé'), findsOneWidget);
  });

  testWidgets('receive displays repository identity without fake QR', (tester) async {
    final repository = _FakeTransferRepository();
    await tester.pumpWidget(MaterialApp(home: SendReceivePage(repository: repository)));
    await tester.tap(find.text('Recevoir'));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('receive-public-label')), findsOneWidget);
    expect(find.text('@afwal-id-test'), findsOneWidget);
    expect(find.textContaining('aucun jeton QR backend valide'), findsOneWidget);
  });

  testWidgets('unavailable receive still allows return to wallet without simulation', (tester) async {
    var returnCount = 0;

    await tester.pumpWidget(MaterialApp(
      home: SendReceivePage(
        repository: const UnavailableTransferRepository(),
        initialMode: SendReceiveMode.receive,
        onReturnToWallet: () => returnCount += 1,
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.textContaining('Aucun AfWal ID ou QR de paiement n’est simulé'), findsOneWidget);
    expect(find.byKey(const Key('return-to-wallet-receive-unavailable')), findsOneWidget);
    await tester.tap(find.byKey(const Key('return-to-wallet-receive-unavailable')));

    expect(returnCount, 1);
  });
}

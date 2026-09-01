import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/payment_transfer.dart';
import 'package:mobile_app/pages/send_receive_page.dart';
import 'package:mobile_app/services/transfer_repository.dart';

class _FakeTransferRepository implements TransferRepository {
  SendTransferRequest? lastRequest;

  @override
  Future<ReceiveIdentity> loadReceiveIdentity() async => const ReceiveIdentity(publicLabel: '@awid-test');

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
  testWidgets('submits transfer through repository only', (tester) async {
    final repository = _FakeTransferRepository();
    await tester.pumpWidget(MaterialApp(home: SendReceivePage(repository: repository)));

    await tester.enterText(find.widgetWithText(TextField, 'AWID ou identifiant destinataire'), '@receiver');
    await tester.enterText(find.widgetWithText(TextField, 'Montant'), '12.50');
    await tester.tap(find.widgetWithText(FilledButton, 'Continuer'));
    await tester.pumpAndSettle();

    expect(repository.lastRequest?.payeeId, '@receiver');
    expect(repository.lastRequest?.amountMinor, 1250);
    expect(find.textContaining('PI-TEST-001'), findsOneWidget);
  });

  testWidgets('unavailable repository never simulates transfer', (tester) async {
    await tester.pumpWidget(const MaterialApp(home: SendReceivePage(repository: UnavailableTransferRepository())));
    await tester.enterText(find.widgetWithText(TextField, 'AWID ou identifiant destinataire'), '@receiver');
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
    expect(find.text('@awid-test'), findsOneWidget);
    expect(find.textContaining('aucun jeton QR backend valide'), findsOneWidget);
  });
}

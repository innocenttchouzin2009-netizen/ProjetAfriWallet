import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/payment_transfer.dart';
import 'package:mobile_app/pages/p2p_transfer_review_page.dart';
import 'package:mobile_app/services/transfer_repository.dart';

class _FakeRepository implements TransferRepository {
  int sendCalls = 0;
  SendTransferRequest? lastRequest;

  @override
  Future<ReceiveIdentity> loadReceiveIdentity() {
    throw UnimplementedError();
  }

  @override
  Future<TransferReceipt> send(SendTransferRequest request) async {
    sendCalls += 1;
    lastRequest = request;
    return TransferReceipt(
      paymentIntentId: 'P2P-REVIEW-001',
      status: TransferStatus.completed,
      amountMinor: request.amountMinor,
      currencyCode: request.currencyCode,
      payeeId: request.payeeId,
    );
  }
}

const _request = SendTransferRequest(
  sourceWalletId: 'wallet-eur',
  recipientKind: TransferRecipientKind.afWalId,
  payeeId: '@receiver',
  amountMinor: 1250,
  currencyCode: 'EUR',
  idempotencyKey: 'idem-review-1',
);

void main() {
  testWidgets('review renders transfer details without executing it', (tester) async {
    final repository = _FakeRepository();

    await tester.pumpWidget(
      MaterialApp(
        home: P2pTransferReviewPage(
          repository: repository,
          request: _request,
        ),
      ),
    );

    expect(find.text('Vérifier le transfert'), findsOneWidget);
    expect(find.text('@receiver'), findsOneWidget);
    expect(find.text('12.50 EUR'), findsOneWidget);
    expect(repository.sendCalls, 0);
  });

  testWidgets('explicit confirmation executes transfer exactly once', (tester) async {
    final repository = _FakeRepository();

    await tester.pumpWidget(
      MaterialApp(
        home: P2pTransferReviewPage(
          repository: repository,
          request: _request,
        ),
      ),
    );

    await tester.tap(find.byKey(const Key('confirm-p2p-transfer')));
    await tester.pumpAndSettle();

    expect(repository.sendCalls, 1);
    expect(repository.lastRequest, same(_request));
    expect(find.byKey(const Key('p2p-transfer-receipt')), findsOneWidget);

    final confirmButton = tester.widget<FilledButton>(
      find.byKey(const Key('confirm-p2p-transfer')),
    );
    expect(confirmButton.onPressed, isNull);
  });

  testWidgets('modify returns without executing transfer', (tester) async {
    final repository = _FakeRepository();

    await tester.pumpWidget(
      MaterialApp(
        home: Builder(
          builder: (context) => Scaffold(
            body: Center(
              child: FilledButton(
                onPressed: () {
                  Navigator.of(context).push(
                    MaterialPageRoute<void>(
                      builder: (_) => P2pTransferReviewPage(
                        repository: repository,
                        request: _request,
                      ),
                    ),
                  );
                },
                child: const Text('Open'),
              ),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('Open'));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('edit-p2p-transfer')));
    await tester.pumpAndSettle();

    expect(repository.sendCalls, 0);
    expect(find.text('Open'), findsOneWidget);
  });
}

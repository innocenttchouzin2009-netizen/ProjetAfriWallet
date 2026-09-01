import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/qr_payment.dart';
import 'package:mobile_app/pages/qr_payment_review_page.dart';
import 'package:mobile_app/services/qr_payment_repository.dart';

class _FakeQrRepository implements QrPaymentRepository {
  _FakeQrRepository({required this.authoritativeResult});

  final QrPaymentResult authoritativeResult;
  int initiateCalls = 0;
  int statusCalls = 0;

  @override
  Future<QrPaymentPayload> decodeAndValidate(String rawCode) =>
      throw UnimplementedError();

  @override
  Future<QrPaymentResult> initiatePayment({
    required QrPaymentPayload payload,
    required String payerWalletId,
  }) async {
    initiateCalls++;
    return const QrPaymentResult(
      status: QrPaymentStatus.initiated,
      transferIntentId: 'transfer-001',
    );
  }

  @override
  Future<QrPaymentResult> getAuthoritativeStatus(String transferIntentId) async {
    statusCalls++;
    return authoritativeResult;
  }
}

void main() {
  const payload = QrPaymentPayload(
    type: QrPaymentType.static,
    merchantId: 'merchant-001',
    amountMinor: 1550,
    currencyCode: 'XAF',
    merchantName: 'Afri Shop',
    description: 'Coffee purchase',
  );

  testWidgets('shows merchant and amount before payment confirmation',
      (tester) async {
    final repository = _FakeQrRepository(
      authoritativeResult:
          const QrPaymentResult(status: QrPaymentStatus.pendingConfirmation),
    );

    await tester.pumpWidget(MaterialApp(
      home: QrPaymentReviewPage(
        payload: payload,
        repository: repository,
        payerWalletId: 'wallet-001',
      ),
    ));

    expect(find.text('Afri Shop'), findsOneWidget);
    expect(find.text('15.50 XAF'), findsOneWidget);
    expect(find.text('Coffee purchase'), findsOneWidget);
    expect(repository.initiateCalls, 0);
  });

  testWidgets('requires backend status before showing payment confirmed',
      (tester) async {
    final repository = _FakeQrRepository(
      authoritativeResult: const QrPaymentResult(
        status: QrPaymentStatus.paid,
        transferIntentId: 'transfer-001',
        receiptCode: 'AFW-RECEIPT',
      ),
    );

    await tester.pumpWidget(MaterialApp(
      home: QrPaymentReviewPage(
        payload: payload,
        repository: repository,
        payerWalletId: 'wallet-001',
      ),
    ));

    await tester.tap(find.byKey(const Key('qr-confirm-payment')));
    await tester.pumpAndSettle();

    expect(repository.initiateCalls, 1);
    expect(repository.statusCalls, 1);
    expect(find.text('Paiement confirmé par le backend'), findsOneWidget);
    expect(find.text('Reçu : AFW-RECEIPT'), findsOneWidget);
  });

  testWidgets('dynamic zero-amount QR cannot be confirmed', (tester) async {
    final repository = _FakeQrRepository(
      authoritativeResult:
          const QrPaymentResult(status: QrPaymentStatus.pendingConfirmation),
    );
    const dynamicPayload = QrPaymentPayload(
      type: QrPaymentType.dynamic,
      merchantId: 'merchant-002',
      amountMinor: 0,
      currencyCode: 'EUR',
      merchantName: 'Afri Market',
      description: 'Open amount',
    );

    await tester.pumpWidget(MaterialApp(
      home: QrPaymentReviewPage(
        payload: dynamicPayload,
        repository: repository,
        payerWalletId: 'wallet-001',
      ),
    ));

    expect(find.text('Montant à définir'), findsOneWidget);
    expect(find.byKey(const Key('qr-dynamic-amount-required')), findsOneWidget);
    expect(
      tester.widget<FilledButton>(find.byKey(const Key('qr-confirm-payment')))
          .onPressed,
      isNull,
    );
  });
}

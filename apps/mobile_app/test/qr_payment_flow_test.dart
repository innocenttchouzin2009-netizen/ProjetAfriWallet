import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/qr_payment.dart';
import 'package:mobile_app/pages/qr_payment_page.dart';
import 'package:mobile_app/services/qr_payment_repository.dart';

class RecordingQrPaymentRepository implements QrPaymentRepository {
  int initiateCalls = 0;
  int statusCalls = 0;

  @override
  Future<QrPaymentPayload> decodeAndValidate(String rawCode) {
    throw UnimplementedError();
  }

  @override
  Future<QrPaymentResult> initiatePayment({
    required QrPaymentPayload payload,
    required String payerWalletId,
  }) async {
    initiateCalls += 1;
    return const QrPaymentResult(
      status: QrPaymentStatus.initiated,
      transferIntentId: 'transfer-001',
    );
  }

  @override
  Future<QrPaymentResult> getAuthoritativeStatus(String transferIntentId) async {
    statusCalls += 1;
    return const QrPaymentResult(status: QrPaymentStatus.pendingConfirmation);
  }
}

void main() {
  testWidgets('valid QR opens review without initiating payment', (tester) async {
    final repository = RecordingQrPaymentRepository();

    await tester.pumpWidget(MaterialApp(
      home: QrPaymentPage(
        repository: repository,
        payerWalletId: 'wallet-test',
        onContinue: () {},
      ),
    ));

    await tester.enterText(
      find.byKey(const Key('qr-test-input')),
      'AFW|Static|merchant-001|15.50|XAF|Afri Shop|Coffee purchase',
    );
    await tester.tap(find.byKey(const Key('validate-qr-test-input')));
    await tester.pumpAndSettle();

    expect(find.text('Vérifiez avant de payer'), findsOneWidget);
    expect(find.text('Afri Shop'), findsOneWidget);
    expect(find.text('15.50 XAF'), findsOneWidget);
    expect(find.text('Coffee purchase'), findsOneWidget);
    expect(find.byKey(const Key('qr-confirm-payment')), findsOneWidget);
    expect(repository.initiateCalls, 0);
    expect(repository.statusCalls, 0);
  });

  testWidgets('invalid QR never reaches review or payment initiation', (tester) async {
    final repository = RecordingQrPaymentRepository();

    await tester.pumpWidget(MaterialApp(
      home: QrPaymentPage(
        repository: repository,
        payerWalletId: 'wallet-test',
        onContinue: () {},
      ),
    ));

    await tester.enterText(
      find.byKey(const Key('qr-test-input')),
      'OTHER|Static|merchant-001|15.50|XAF',
    );
    await tester.tap(find.byKey(const Key('validate-qr-test-input')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('qr-validation-error')), findsOneWidget);
    expect(find.text('Vérifiez avant de payer'), findsNothing);
    expect(repository.initiateCalls, 0);
    expect(repository.statusCalls, 0);
  });

  testWidgets('dynamic zero amount reaches review but confirm remains disabled',
      (tester) async {
    final repository = RecordingQrPaymentRepository();

    await tester.pumpWidget(MaterialApp(
      home: QrPaymentPage(
        repository: repository,
        payerWalletId: 'wallet-test',
        onContinue: () {},
      ),
    ));

    await tester.enterText(
      find.byKey(const Key('qr-test-input')),
      'AFW|Dynamic|merchant-002|0|EUR|Afri Market|Open amount',
    );
    await tester.tap(find.byKey(const Key('validate-qr-test-input')));
    await tester.pumpAndSettle();

    expect(find.text('Vérifiez avant de payer'), findsOneWidget);
    expect(find.byKey(const Key('qr-dynamic-amount-required')), findsOneWidget);
    final button = tester.widget<FilledButton>(
      find.byKey(const Key('qr-confirm-payment')),
    );
    expect(button.onPressed, isNull);
    expect(repository.initiateCalls, 0);
  });
}

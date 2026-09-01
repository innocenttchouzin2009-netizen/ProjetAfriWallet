import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/qr_payment.dart';
import 'package:mobile_app/pages/qr_payment_review_page.dart';

void main() {
  testWidgets('shows merchant and amount before explicit confirmation', (tester) async {
    var confirmations = 0;
    const payload = QrPaymentPayload(
      type: QrPaymentType.static,
      merchantId: 'merchant-001',
      amountMinor: 1550,
      currencyCode: 'XAF',
      merchantName: 'Afri Shop',
      description: 'Coffee purchase',
    );

    await tester.pumpWidget(
      MaterialApp(
        home: QrPaymentReviewPage(
          payload: payload,
          onConfirm: () async {
            confirmations += 1;
          },
        ),
      ),
    );

    expect(find.text('Vérifiez avant de payer'), findsOneWidget);
    expect(find.text('Afri Shop'), findsOneWidget);
    expect(find.text('15.50 XAF'), findsOneWidget);
    expect(find.text('Coffee purchase'), findsOneWidget);
    expect(confirmations, 0);

    await tester.tap(find.text('Confirmer le paiement'));
    await tester.pump();

    expect(confirmations, 1);
  });

  testWidgets('dynamic QR never invents a payable amount', (tester) async {
    const payload = QrPaymentPayload(
      type: QrPaymentType.dynamic,
      merchantId: 'merchant-002',
      amountMinor: 0,
      currencyCode: 'EUR',
      merchantName: 'Afri Market',
      description: 'Open amount',
    );

    await tester.pumpWidget(
      MaterialApp(
        home: QrPaymentReviewPage(
          payload: payload,
          onConfirm: () async {},
        ),
      ),
    );

    expect(find.text('Montant à saisir'), findsOneWidget);
    expect(find.text('0.00 EUR'), findsNothing);
  });

  testWidgets('submitting state disables duplicate confirmation', (tester) async {
    var confirmations = 0;
    const payload = QrPaymentPayload(
      type: QrPaymentType.static,
      merchantId: 'merchant-003',
      amountMinor: 1000,
      currencyCode: 'EUR',
      merchantName: 'Afri Store',
      description: '',
    );

    await tester.pumpWidget(
      MaterialApp(
        home: QrPaymentReviewPage(
          payload: payload,
          isSubmitting: true,
          onConfirm: () async {
            confirmations += 1;
          },
        ),
      ),
    );

    expect(find.text('Traitement…'), findsOneWidget);
    await tester.tap(find.text('Traitement…'));
    await tester.pump();
    expect(confirmations, 0);
  });
}

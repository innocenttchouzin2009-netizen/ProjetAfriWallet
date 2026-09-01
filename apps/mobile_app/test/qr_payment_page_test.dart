import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/qr_payment.dart';
import 'package:mobile_app/pages/qr_payment_page.dart';
import 'package:mobile_app/services/qr_payment_repository.dart';

class FakeQrPaymentRepository implements QrPaymentRepository {
  @override
  Future<QrPaymentPayload> decodeAndValidate(String rawCode) {
    throw UnimplementedError();
  }

  @override
  Future<QrPaymentResult> initiatePayment({
    required QrPaymentPayload payload,
    required String payerWalletId,
  }) {
    throw UnimplementedError();
  }

  @override
  Future<QrPaymentResult> getAuthoritativeStatus(String transferIntentId) {
    throw UnimplementedError();
  }
}

void main() {
  testWidgets('camera-first page keeps test validation as a non-payment path',
      (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: QrPaymentPage(
          repository: FakeQrPaymentRepository(),
          onContinue: () {},
        ),
      ),
    );

    expect(find.text('Scanner avec la caméra'), findsOneWidget);
    expect(find.text('Valider la saisie de test'), findsOneWidget);

    await tester.enterText(
      find.byType(TextField),
      'AFW|Static|merchant-001|15.50|XAF|Afri Shop|Coffee purchase',
    );
    await tester.tap(find.text('Valider la saisie de test'));
    await tester.pump();

    expect(find.text('Vérifier avant paiement'), findsOneWidget);
    expect(find.text('Afri Shop'), findsOneWidget);
    expect(find.text('15.50 XAF'), findsOneWidget);
    expect(find.text('Merchant ID : merchant-001'), findsOneWidget);

    final backendNotice =
        find.textContaining('confirmation financière devra provenir du backend');
    await tester.scrollUntilVisible(
      backendNotice,
      200,
      scrollable: find.byType(Scrollable).first,
    );
    expect(backendNotice, findsOneWidget);
    expect(find.text('Paiement réussi'), findsNothing);
  });

  testWidgets('invalid test QR never reaches review', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: QrPaymentPage(
          repository: FakeQrPaymentRepository(),
          onContinue: () {},
        ),
      ),
    );

    await tester.enterText(find.byType(TextField), 'NOT-AFW|bad');
    await tester.tap(find.text('Valider la saisie de test'));
    await tester.pump();

    expect(find.textContaining('QR invalide'), findsOneWidget);
    expect(find.text('Vérifier avant paiement'), findsNothing);
    expect(find.text('Paiement réussi'), findsNothing);
  });
}

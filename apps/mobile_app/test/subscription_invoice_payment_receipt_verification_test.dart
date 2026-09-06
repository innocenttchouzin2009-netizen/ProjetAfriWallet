import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/l10n/app_localizations.dart';
import 'package:mobile_app/models/subscription_models.dart';
import 'package:mobile_app/models/subscription_receipt_verification.dart';
import 'package:mobile_app/pages/subscription_invoice_payment_entry_page.dart';
import 'package:mobile_app/pages/subscription_receipt_verification_page.dart';
import 'package:mobile_app/services/subscription_receipt_verification_codec.dart';

const _codec = SubscriptionReceiptVerificationCodec();
const _invoice = SubscriptionInvoice(
  id: 'invoice-beta133-pending',
  subscriptionId: 'subscription-beta133',
  amount: 42.50,
  currency: 'EUR',
  status: 'PENDING',
  issueDate: '2026-09-06',
);

Widget _localized(Widget home) => MaterialApp(
      locale: const Locale('en'),
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: home,
    );

String _code({required int issuedAt, required int expiresAt}) => _codec.encode(
      SubscriptionReceiptVerificationPayload(
        version: SubscriptionReceiptVerificationCodec.currentVersion,
        invoiceId: _invoice.id,
        paymentReference: 'BETA-${_invoice.id}',
        status: SubscriptionReceiptVerificationStatus.success,
        issuedAtEpochSeconds: issuedAt,
        expiresAtEpochSeconds: expiresAt,
      ),
    );

Future<void> _ensureVisible(WidgetTester tester, Finder finder) async {
  final listView = find.byKey(
    const Key('subscription-invoice-payment-entry-page'),
  );
  for (var index = 0; index < 20 && finder.evaluate().isEmpty; index++) {
    await tester.drag(listView, const Offset(0, -240));
    await tester.pumpAndSettle();
  }
  expect(finder, findsOneWidget);
  await tester.ensureVisible(finder);
  await tester.pumpAndSettle();
}

Future<void> _openReceipt(WidgetTester tester) async {
  await tester.pumpWidget(
    _localized(const SubscriptionInvoicePaymentEntryPage(invoice: _invoice)),
  );
  await tester.pumpAndSettle();

  final method = find.byKey(const Key('invoice-payment-method-wallet'));
  await _ensureVisible(tester, method);
  await tester.tap(method);
  await tester.pumpAndSettle();

  final continueButton = find.byKey(const Key('invoice-payment-continue'));
  await _ensureVisible(tester, continueButton);
  await tester.tap(continueButton);
  await tester.pumpAndSettle();

  final confirmButton = find.byKey(const Key('invoice-payment-confirm'));
  await _ensureVisible(tester, confirmButton);
  await tester.tap(confirmButton);
  await tester.pump(const Duration(milliseconds: 300));
  await tester.pumpAndSettle();

  final viewReceipt = find.byKey(const Key('invoice-payment-view-receipt'));
  await _ensureVisible(tester, viewReceipt);
  await tester.tap(viewReceipt);
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('receipt verification screen shows valid state', (tester) async {
    final now = DateTime.fromMillisecondsSinceEpoch(2000 * 1000, isUtc: true);
    await tester.pumpWidget(
      _localized(
        SubscriptionReceiptVerificationPage(
          rawCode: _code(issuedAt: 1000, expiresAt: 3000),
          nowProvider: () => now,
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('receipt-verification-valid')), findsOneWidget);
    expect(find.text('Valid receipt'), findsOneWidget);
    expect(find.text(_invoice.id), findsOneWidget);
  });

  testWidgets('receipt verification screen shows invalid state', (tester) async {
    await tester.pumpWidget(
      _localized(
        SubscriptionReceiptVerificationPage(
          rawCode: 'AFW|static|merchant|10.00|EUR',
          nowProvider: () => DateTime.utc(2026, 9, 6),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('receipt-verification-invalid')), findsOneWidget);
    expect(find.text('Invalid receipt'), findsOneWidget);
  });

  testWidgets('receipt verification screen shows expired state', (tester) async {
    final now = DateTime.fromMillisecondsSinceEpoch(4000 * 1000, isUtc: true);
    await tester.pumpWidget(
      _localized(
        SubscriptionReceiptVerificationPage(
          rawCode: _code(issuedAt: 1000, expiresAt: 3000),
          nowProvider: () => now,
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('receipt-verification-expired')), findsOneWidget);
    expect(find.text('Expired receipt'), findsOneWidget);
  });

  testWidgets('successful receipt renders QR and opens local verification',
      (tester) async {
    await _openReceipt(tester);

    final qr = find.byKey(const Key('invoice-payment-receipt-verification-qr'));
    await _ensureVisible(tester, qr);
    expect(qr, findsOneWidget);

    final verify = find.byKey(const Key('invoice-payment-receipt-verify'));
    await _ensureVisible(tester, verify);
    await tester.tap(verify);
    await tester.pumpAndSettle();

    expect(
      find.byKey(const Key('subscription-receipt-verification-page')),
      findsOneWidget,
    );
    expect(find.byKey(const Key('receipt-verification-valid')), findsOneWidget);
  });
}

import 'package:flutter_test/flutter_test.dart';
import 'package:merchant_dashboard/main.dart';

void main() {
  testWidgets('merchant dashboard shows key sections', (tester) async {
    await tester.pumpWidget(const MerchantDashboardApp());

    expect(find.text('Merchant Dashboard'), findsOneWidget);
    expect(find.text('Revenue analytics'), findsOneWidget);
    expect(find.text('Recent transactions'), findsOneWidget);
    expect(find.text('QR / POS overview'), findsOneWidget);
  });
}

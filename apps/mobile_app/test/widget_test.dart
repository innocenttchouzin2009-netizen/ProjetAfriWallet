import 'package:flutter_test/flutter_test.dart';
import 'package:afriwallet_mobile/app/afriwallet_app.dart';

void main() {
  testWidgets('AfriCard screen loads', (WidgetTester tester) async {
    await tester.pumpWidget(const AfriWalletApp());

    expect(find.text('AfriCard'), findsOneWidget);
    expect(find.text('@innocent'), findsOneWidget);
    expect(find.text('Secure QR Identity'), findsOneWidget);
  });
}

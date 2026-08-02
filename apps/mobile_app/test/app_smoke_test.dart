import 'package:afriwallet_mobile/app/afriwallet_app.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('AfriWallet app starts', (WidgetTester tester) async {
    await tester.pumpWidget(const ProviderScope(child: AfriWalletApp()));

    expect(find.text('AfriWallet'), findsWidgets);
  });
}

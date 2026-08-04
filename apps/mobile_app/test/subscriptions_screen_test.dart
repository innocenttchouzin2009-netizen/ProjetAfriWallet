import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:afriwallet_mobile/features/subscriptions/presentation/screens/subscriptions_screen.dart';

void main() {
  testWidgets(
    'displays the initial subscription providers',
    (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: SubscriptionsScreen(),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.text('Abonnements'), findsOneWidget);
      expect(find.text('Netflix'), findsOneWidget);
      expect(find.text('Canal+'), findsOneWidget);
      expect(find.text('MyBouquetAfricain'), findsOneWidget);
      expect(find.text('Cinaf'), findsOneWidget);
    },
  );
}

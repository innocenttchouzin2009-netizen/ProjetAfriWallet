import 'package:afw_design_system/afw_design_system.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('financial timeline renders events', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: FinancialTimeline(
            sectionTitle: "Aujourd'hui",
            events: [
              FinancialTimelineEvent(
                title: 'Salaire',
                amount: 250,
                timeLabel: '08:00',
                currency: 'EUR',
                isCredit: true,
              ),
              FinancialTimelineEvent(
                title: 'Restaurant',
                amount: 50,
                timeLabel: '12:45',
                currency: 'EUR',
                isCredit: false,
              ),
            ],
          ),
        ),
      ),
    );

    expect(find.text("Aujourd'hui"), findsOneWidget);
    expect(find.text('Salaire'), findsOneWidget);
    expect(find.text('Restaurant'), findsOneWidget);
    expect(find.text('+250 EUR'), findsOneWidget);
    expect(find.text('-50 EUR'), findsOneWidget);
  });
}

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:mobile_app/pages/onboarding_auth_page.dart';
import 'package:mobile_app/theme/afwal_theme.dart';

void main() {
  testWidgets('onboarding advances to account access', (tester) async {
    await tester.pumpWidget(MaterialApp(
      theme: AfWalTheme.light(),
      home: OnboardingAuthPage(onContinueToBeta: () {}),
    ));

    expect(find.text('Une identité pour une Afrique connectée'), findsOneWidget);
    await tester.tap(find.text('Continuer'));
    await tester.pumpAndSettle();
    expect(find.text('Payez et recevez simplement'), findsOneWidget);

    await tester.tap(find.text('Continuer'));
    await tester.pumpAndSettle();
    expect(find.text('La sécurité dès le départ'), findsOneWidget);

    await tester.tap(find.text('Commencer'));
    await tester.pumpAndSettle();
    expect(find.text('Bienvenue sur AfWal'), findsOneWidget);
    expect(find.text('Créer un compte'), findsOneWidget);
    expect(find.text('Se connecter'), findsOneWidget);
  });

  testWidgets('account form rejects empty access data', (tester) async {
    await tester.pumpWidget(MaterialApp(
      theme: AfWalTheme.light(),
      home: AuthFormPage(
        createAccount: false,
        onContinueToBeta: () {},
      ),
    ));

    final submitButton = find.widgetWithText(FilledButton, 'Se connecter');
    expect(submitButton, findsOneWidget);
    await tester.tap(submitButton);
    await tester.pump();

    expect(find.text('Saisissez votre téléphone ou e-mail.'), findsOneWidget);
    expect(find.text('Le PIN doit contenir au moins 6 chiffres.'), findsOneWidget);
  });
}

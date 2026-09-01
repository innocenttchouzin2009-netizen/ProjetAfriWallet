import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/awid_profile.dart';
import 'package:mobile_app/pages/identity_awid_page.dart';
import 'package:mobile_app/services/identity_repository.dart';

class _ReadyIdentityRepository implements IdentityRepository {
  @override
  Future<AwidProfile> loadCurrentProfile() async {
    return const AwidProfile(
      awid: 'AWID-TEST-001',
      alias: '@testuser',
      displayName: 'Test User',
      isPrivate: false,
    );
  }
}

class _UnavailableIdentityRepository implements IdentityRepository {
  @override
  Future<AwidProfile> loadCurrentProfile() async {
    throw const IdentityUnavailableException();
  }
}

void main() {
  testWidgets('renders real repository identity payload', (tester) async {
    await tester.pumpWidget(MaterialApp(
      home: IdentityAwidPage(repository: _ReadyIdentityRepository(), onContinue: () {}),
    ));

    await tester.pumpAndSettle();

    expect(find.text('Mon AfWal ID'), findsOneWidget);
    expect(find.text('@testuser'), findsOneWidget);
    expect(find.text('Test User'), findsOneWidget);
    expect(find.text('Profil public'), findsOneWidget);
    expect(find.text('Copier mon AfWal ID'), findsOneWidget);
  });

  testWidgets('never fabricates an identity when backend data is unavailable', (tester) async {
    await tester.pumpWidget(MaterialApp(
      home: IdentityAwidPage(repository: _UnavailableIdentityRepository(), onContinue: () {}),
    ));

    await tester.pumpAndSettle();

    expect(find.text('AfWal ID indisponible'), findsOneWidget);
    expect(find.textContaining('Aucune identité n’est simulée'), findsOneWidget);
    expect(find.text('Continuer sans afficher mon ID'), findsOneWidget);
  });
}

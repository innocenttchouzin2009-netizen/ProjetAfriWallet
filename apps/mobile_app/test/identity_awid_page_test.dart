import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/models/current_identity_profile.dart';
import 'package:mobile_app/models/payment_transfer.dart';
import 'package:mobile_app/models/public_afwal_identity.dart';
import 'package:mobile_app/pages/identity_awid_page.dart';
import 'package:mobile_app/services/identity_read_repository.dart';
import 'package:mobile_app/services/transfer_repository.dart';

class _ReadyIdentityReadRepository implements IdentityReadRepository {
  @override
  Future<CurrentIdentityProfile> loadCurrentProfile() async {
    return CurrentIdentityProfile(
      userId: 'user-1',
      identifier: 'user@example.com',
      createdAtUtc: DateTime.utc(2026, 10, 1, 9, 30),
    );
  }

  @override
  Future<PublicAfWalIdentity> loadPublicAfWalId(String afWalId) async {
    return PublicAfWalIdentity(afWalId: afWalId);
  }
}

class _UnavailableIdentityReadRepository implements IdentityReadRepository {
  @override
  Future<CurrentIdentityProfile> loadCurrentProfile() async {
    throw const IdentityReadUnavailableException();
  }

  @override
  Future<PublicAfWalIdentity> loadPublicAfWalId(String afWalId) async {
    throw const IdentityReadUnavailableException();
  }
}

class _ReadyTransferRepository implements TransferRepository {
  @override
  Future<ReceiveIdentity> loadReceiveIdentity() async {
    return const ReceiveIdentity(
      publicLabel: '@testuser',
      qrToken: 'qr-token',
    );
  }

  @override
  Future<TransferReceipt> send(SendTransferRequest request) {
    throw UnimplementedError();
  }
}

class _UnavailableTransferRepository implements TransferRepository {
  @override
  Future<ReceiveIdentity> loadReceiveIdentity() async {
    throw const TransferUnavailableException('unavailable');
  }

  @override
  Future<TransferReceipt> send(SendTransferRequest request) {
    throw UnimplementedError();
  }
}

void main() {
  testWidgets('renders authoritative backend identity payload', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: IdentityAwidPage(
          identityRepository: _ReadyIdentityReadRepository(),
          transferRepository: _ReadyTransferRepository(),
          onContinue: () {},
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Mon AfWal ID'), findsOneWidget);
    expect(find.text('@testuser'), findsOneWidget);
    expect(find.text('user@example.com'), findsOneWidget);
    expect(find.text('Identité vérifiée par le backend'), findsOneWidget);
    expect(find.text('Copier mon AfWal ID'), findsOneWidget);
    expect(
      find.text('Un jeton QR sécurisé est disponible depuis le backend.'),
      findsOneWidget,
    );
  });

  testWidgets('never fabricates identity when backend data is unavailable',
      (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: IdentityAwidPage(
          identityRepository: _UnavailableIdentityReadRepository(),
          transferRepository: _UnavailableTransferRepository(),
          onContinue: () {},
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('AfWal ID indisponible'), findsOneWidget);
    expect(
      find.textContaining('services backend autoritatifs'),
      findsOneWidget,
    );
    expect(find.text('Continuer sans afficher mon ID'), findsOneWidget);
    expect(find.text('@testuser'), findsNothing);
  });
}

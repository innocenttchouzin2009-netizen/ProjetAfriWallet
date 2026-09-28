import '../data/remote/p2p_remote_data_source.dart';
import '../models/auth_session.dart';
import '../models/payment_transfer.dart';
import 'auth_session_coordinator.dart';
import 'secure_session_store.dart';

abstract interface class TransferRepository {
  Future<TransferReceipt> send(SendTransferRequest request);
  Future<ReceiveIdentity> loadReceiveIdentity();
}

class TransferUnavailableException implements Exception {
  const TransferUnavailableException(this.message);
  final String message;

  @override
  String toString() => message;
}

class UnavailableTransferRepository implements TransferRepository {
  const UnavailableTransferRepository();

  @override
  Future<TransferReceipt> send(SendTransferRequest request) async {
    throw const TransferUnavailableException(
      'Le service de transfert n’est pas connecté. Aucun transfert n’a été simulé.',
    );
  }

  @override
  Future<ReceiveIdentity> loadReceiveIdentity() async {
    throw const TransferUnavailableException(
      'L’identité de réception n’est pas disponible. Aucun QR n’a été simulé.',
    );
  }
}

class AuthenticatedTransferRepository implements TransferRepository {
  const AuthenticatedTransferRepository(
    this._remoteDataSource,
    this._sessionStore,
  ) : _sessionLifecycle = null;

  const AuthenticatedTransferRepository.withSessionLifecycle(
    this._remoteDataSource,
    this._sessionLifecycle,
  ) : _sessionStore = null;

  final P2PRemoteDataSource _remoteDataSource;
  final AuthSessionStore? _sessionStore;
  final AuthSessionLifecycle? _sessionLifecycle;

  @override
  Future<TransferReceipt> send(SendTransferRequest request) async {
    final accessToken = await _requireAccessToken();
    final response = await _remoteDataSource.executeTransfer(
      accessToken,
      P2PTransferRequest(
        sourceWalletId: request.sourceWalletId,
        recipientKind: switch (request.recipientKind) {
          TransferRecipientKind.afWalId => P2PRecipientKind.afWalId,
          TransferRecipientKind.qr => P2PRecipientKind.qr,
        },
        recipientValue: request.payeeId,
        currencyCode: request.currencyCode,
        amountMinor: request.amountMinor,
        correlationId: request.idempotencyKey,
      ),
    );

    return TransferReceipt(
      paymentIntentId: response.transferId,
      status: TransferStatus.completed,
      amountMinor: response.amountMinor,
      currencyCode: response.currencyCode,
      payeeId: request.payeeId,
    );
  }

  @override
  Future<ReceiveIdentity> loadReceiveIdentity() async {
    final accessToken = await _requireAccessToken();
    final response = await _remoteDataSource.issueReceiveIdentity(accessToken);

    return ReceiveIdentity(
      publicLabel: response.publicLabel,
      qrToken: response.qrToken,
    );
  }

  Future<String> _requireAccessToken() async {
    final session = await _restoreValidSession();
    if (session == null) {
      throw const TransferUnavailableException(
        'Une session authentifiée valide est requise pour utiliser les transferts.',
      );
    }

    return session.accessToken;
  }

  Future<StoredAuthSession?> _restoreValidSession() async {
    final lifecycle = _sessionLifecycle;
    if (lifecycle != null) {
      return lifecycle.restoreValidSession();
    }

    final session = await _sessionStore!.read();
    if (session == null ||
        session.isAccessTokenExpired(DateTime.now().toUtc())) {
      return null;
    }

    return session;
  }
}

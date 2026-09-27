import '../data/remote/p2p_remote_data_source.dart';
import '../models/payment_transfer.dart';
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
  );

  final P2PRemoteDataSource _remoteDataSource;
  final AuthSessionStore _sessionStore;

  @override
  Future<TransferReceipt> send(SendTransferRequest request) async {
    final accessToken = await _requireAccessToken();
    final response = await _remoteDataSource.executeTransfer(
      accessToken,
      P2PTransferRequest(
        sourceWalletId: request.payerId,
        recipientKind: P2PRecipientKind.afWalId,
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
    final session = await _sessionStore.read();
    if (session == null) {
      throw const TransferUnavailableException(
        'Une session authentifiée est requise pour utiliser les transferts.',
      );
    }

    return session.accessToken;
  }
}

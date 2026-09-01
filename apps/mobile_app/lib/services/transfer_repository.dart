import '../models/payment_transfer.dart';

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

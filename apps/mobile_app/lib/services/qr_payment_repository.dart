import 'dart:convert';
import 'dart:math';

import '../data/remote/qr_payment_remote_data_source.dart';
import '../models/qr_payment.dart';
import 'secure_session_store.dart';

abstract interface class QrPaymentRepository {
  Future<QrPaymentPayload> decodeAndValidate(String rawCode);

  Future<QrPaymentResult> initiatePayment({
    required QrPaymentPayload payload,
    required String payerWalletId,
  });

  Future<QrPaymentResult> getAuthoritativeStatus(String transferIntentId);
}

class QrPaymentUnavailableException implements Exception {
  const QrPaymentUnavailableException(this.message);

  final String message;

  @override
  String toString() => message;
}

class InvalidQrPaymentException implements Exception {
  const InvalidQrPaymentException(this.message);

  final String message;

  @override
  String toString() => message;
}

class UnavailableQrPaymentRepository implements QrPaymentRepository {
  const UnavailableQrPaymentRepository();

  static const _message =
      'QR payments are unavailable. No payment result is simulated.';

  @override
  Future<QrPaymentPayload> decodeAndValidate(String rawCode) {
    if (rawCode.trim().isEmpty) {
      return Future<QrPaymentPayload>.error(
        const InvalidQrPaymentException('The QR code is empty.'),
      );
    }

    return Future<QrPaymentPayload>.error(
      const QrPaymentUnavailableException(_message),
    );
  }

  @override
  Future<QrPaymentResult> initiatePayment({
    required QrPaymentPayload payload,
    required String payerWalletId,
  }) {
    return Future<QrPaymentResult>.error(
      const QrPaymentUnavailableException(_message),
    );
  }

  @override
  Future<QrPaymentResult> getAuthoritativeStatus(String transferIntentId) {
    return Future<QrPaymentResult>.error(
      const QrPaymentUnavailableException(_message),
    );
  }
}

typedef QrPaymentIdempotencyKeyFactory = String Function();

class AuthenticatedQrPaymentRepository implements QrPaymentRepository {
  AuthenticatedQrPaymentRepository(
    this._remoteDataSource,
    this._sessionStore, {
    QrPaymentIdempotencyKeyFactory? idempotencyKeyFactory,
  }) : _idempotencyKeyFactory =
           idempotencyKeyFactory ?? _generateIdempotencyKey;

  final QrPaymentRemoteDataSource _remoteDataSource;
  final AuthSessionStore _sessionStore;
  final QrPaymentIdempotencyKeyFactory _idempotencyKeyFactory;

  @override
  Future<QrPaymentPayload> decodeAndValidate(String rawCode) async {
    final normalized = rawCode.trim();
    if (normalized.isEmpty) {
      throw const InvalidQrPaymentException('The QR code is empty.');
    }

    final decoded = await _remoteDataSource.decode(normalized);
    if (decoded.status != QrPaymentStatus.active) {
      throw const InvalidQrPaymentException(
        'The QR payment is not active.',
      );
    }

    final payload = decoded.toPayload();
    if (payload.isExpired) {
      throw const InvalidQrPaymentException(
        'The QR payment has expired.',
      );
    }

    return payload;
  }

  @override
  Future<QrPaymentResult> initiatePayment({
    required QrPaymentPayload payload,
    required String payerWalletId,
  }) async {
    final accessToken = await _requireAccessToken();
    final qrId = _requireQrId(payload);
    final normalizedWalletId = payerWalletId.trim();

    if (normalizedWalletId.isEmpty) {
      throw const InvalidQrPaymentException(
        'A payer wallet is required for QR payment.',
      );
    }
    if (payload.amountMinor <= 0) {
      throw const InvalidQrPaymentException(
        'A positive payment amount is required.',
      );
    }
    if (payload.currencyCode.trim().isEmpty) {
      throw const InvalidQrPaymentException(
        'A payment currency is required.',
      );
    }
    if (payload.isExpired) {
      throw const InvalidQrPaymentException(
        'The QR payment has expired.',
      );
    }

    final response = await _remoteDataSource.initiate(
      accessToken,
      QrPaymentInitiateRequest(
        qrId: qrId,
        payerWalletId: normalizedWalletId,
        amountMinor: payload.amountMinor,
        currency: payload.currencyCode,
        idempotencyKey: _idempotencyKeyFactory(),
      ),
    );

    return response.toResult();
  }

  @override
  Future<QrPaymentResult> getAuthoritativeStatus(
    String transferIntentId,
  ) async {
    final normalizedTransferIntentId = transferIntentId.trim();
    if (normalizedTransferIntentId.isEmpty) {
      throw const InvalidQrPaymentException(
        'A transfer intent is required to query QR payment status.',
      );
    }

    final accessToken = await _requireAccessToken();
    final response = await _remoteDataSource.getStatus(
      accessToken,
      normalizedTransferIntentId,
    );
    return response.toResult();
  }

  String _requireQrId(QrPaymentPayload payload) {
    final qrId = payload.qrId?.trim();
    if (qrId == null || qrId.isEmpty) {
      throw const InvalidQrPaymentException(
        'A backend-issued QR identifier is required for payment.',
      );
    }
    return qrId;
  }

  Future<String> _requireAccessToken() async {
    final session = await _sessionStore.read();
    if (session == null || session.accessToken.trim().isEmpty) {
      throw const QrPaymentUnavailableException(
        'An authenticated session is required for QR payments.',
      );
    }

    return session.accessToken;
  }

  static String _generateIdempotencyKey() {
    final random = Random.secure();
    final bytes = List<int>.generate(24, (_) => random.nextInt(256));
    return base64UrlEncode(bytes).replaceAll('=', '');
  }
}

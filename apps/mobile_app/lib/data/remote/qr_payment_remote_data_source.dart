import '../../models/qr_payment.dart';
import '../../network/api_client.dart';
import '../../network/api_exception.dart';

class QrPaymentInitiateRequest {
  const QrPaymentInitiateRequest({
    required this.qrId,
    required this.payerWalletId,
    required this.amountMinor,
    required this.currency,
    required this.idempotencyKey,
  });

  final String qrId;
  final String payerWalletId;
  final int amountMinor;
  final String currency;
  final String idempotencyKey;

  Map<String, Object?> toJson() => <String, Object?>{
        'qrId': qrId,
        'payerWalletId': payerWalletId,
        'amountMinor': amountMinor,
        'currency': currency,
        'idempotencyKey': idempotencyKey,
      };
}

class QrPaymentDecodedResponse {
  const QrPaymentDecodedResponse({
    required this.qrId,
    required this.type,
    required this.merchantId,
    required this.amountMinor,
    required this.currency,
    required this.merchantName,
    required this.description,
    required this.status,
    required this.expiresAt,
  });

  final String qrId;
  final QrPaymentType type;
  final String merchantId;
  final int amountMinor;
  final String currency;
  final String merchantName;
  final String description;
  final QrPaymentStatus status;
  final DateTime? expiresAt;

  QrPaymentPayload toPayload() => QrPaymentPayload(
        type: type,
        merchantId: merchantId,
        amountMinor: amountMinor,
        currencyCode: currency,
        merchantName: merchantName,
        description: description,
        qrId: qrId,
        expiresAt: expiresAt,
      );
}

class QrPaymentStatusResponse {
  const QrPaymentStatusResponse({
    required this.paymentId,
    required this.qrId,
    required this.transferIntentId,
    required this.status,
    required this.amountMinor,
    required this.currency,
    required this.receiptId,
    required this.receiptCode,
    required this.updatedAt,
  });

  final String paymentId;
  final String qrId;
  final String transferIntentId;
  final QrPaymentStatus status;
  final int amountMinor;
  final String currency;
  final String? receiptId;
  final String? receiptCode;
  final DateTime updatedAt;

  QrPaymentResult toResult() => QrPaymentResult(
        status: status,
        transferIntentId: transferIntentId,
        receiptId: receiptId,
        receiptCode: receiptCode,
      );
}

class QrPaymentRemoteDataSource {
  const QrPaymentRemoteDataSource(this._apiClient);

  static const String _basePath = '/api/v1/qr-payments';

  final ApiClient _apiClient;

  Future<QrPaymentDecodedResponse> decode(String rawCode) async {
    final payload = await _apiClient.postJson(
      '$_basePath/decode',
      body: <String, Object?>{'code': rawCode},
    );

    try {
      final json = _requireObject(payload);
      return QrPaymentDecodedResponse(
        qrId: _requireString(json, 'qrId'),
        type: _parseType(_requireString(json, 'type')),
        merchantId: _requireString(json, 'merchantId'),
        amountMinor: _requireInt(json, 'amountMinor'),
        currency: _requireString(json, 'currency'),
        merchantName: _requireString(json, 'merchantName', allowEmpty: true),
        description: _requireString(json, 'description', allowEmpty: true),
        status: _parseStatus(_requireString(json, 'status')),
        expiresAt: _optionalDateTimeUtc(json, 'expiresAt'),
      );
    } on FormatException {
      throw const ApiMalformedResponseException(
        'The QR payment decode endpoint returned an invalid response.',
      );
    }
  }

  Future<QrPaymentStatusResponse> initiate(
    String accessToken,
    QrPaymentInitiateRequest request,
  ) async {
    final payload = await _apiClient.postJson(
      '$_basePath/initiate',
      body: request.toJson(),
      headers: _bearerHeaders(accessToken),
    );

    return _parseStatusResponse(
      payload,
      errorMessage:
          'The QR payment initiate endpoint returned an invalid response.',
    );
  }

  Future<QrPaymentStatusResponse> getStatus(
    String accessToken,
    String transferIntentId,
  ) async {
    final payload = await _apiClient.getJson(
      '$_basePath/transfers/$transferIntentId/status',
      headers: _bearerHeaders(accessToken),
    );

    return _parseStatusResponse(
      payload,
      errorMessage:
          'The QR payment status endpoint returned an invalid response.',
    );
  }

  QrPaymentStatusResponse _parseStatusResponse(
    Object? payload, {
    required String errorMessage,
  }) {
    try {
      final json = _requireObject(payload);
      return QrPaymentStatusResponse(
        paymentId: _requireString(json, 'paymentId'),
        qrId: _requireString(json, 'qrId'),
        transferIntentId: _requireString(json, 'transferIntentId'),
        status: _parseStatus(_requireString(json, 'status')),
        amountMinor: _requireInt(json, 'amountMinor'),
        currency: _requireString(json, 'currency'),
        receiptId: _optionalString(json, 'receiptId'),
        receiptCode: _optionalString(json, 'receiptCode'),
        updatedAt: _requireDateTimeUtc(json, 'updatedAt'),
      );
    } on FormatException {
      throw ApiMalformedResponseException(errorMessage);
    }
  }

  Map<String, String> _bearerHeaders(String accessToken) => <String, String>{
        'Authorization': 'Bearer $accessToken',
      };

  Map<String, dynamic> _requireObject(Object? payload) {
    if (payload is Map<String, dynamic>) {
      return payload;
    }
    throw const FormatException('Expected a JSON object.');
  }

  String _requireString(
    Map<String, dynamic> json,
    String key, {
    bool allowEmpty = false,
  }) {
    final value = json[key];
    if (value is String && (allowEmpty || value.trim().isNotEmpty)) {
      return value;
    }
    throw FormatException('Missing or invalid $key.');
  }

  String? _optionalString(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value == null) {
      return null;
    }
    if (value is String) {
      return value;
    }
    throw FormatException('Invalid $key.');
  }

  int _requireInt(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value is int) {
      return value;
    }
    throw FormatException('Missing or invalid $key.');
  }

  DateTime _requireDateTimeUtc(Map<String, dynamic> json, String key) {
    final raw = _requireString(json, key);
    final parsed = DateTime.tryParse(raw);
    if (parsed == null) {
      throw FormatException('Missing or invalid $key.');
    }
    return parsed.toUtc();
  }

  DateTime? _optionalDateTimeUtc(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value == null) {
      return null;
    }
    if (value is! String) {
      throw FormatException('Invalid $key.');
    }
    final parsed = DateTime.tryParse(value);
    if (parsed == null) {
      throw FormatException('Invalid $key.');
    }
    return parsed.toUtc();
  }

  QrPaymentType _parseType(String value) {
    return switch (value) {
      'Static' => QrPaymentType.static,
      'Dynamic' => QrPaymentType.dynamic,
      _ => throw const FormatException('Invalid QR payment type.'),
    };
  }

  QrPaymentStatus _parseStatus(String value) {
    return switch (value) {
      'Active' => QrPaymentStatus.active,
      'Initiated' => QrPaymentStatus.initiated,
      'Paid' => QrPaymentStatus.paid,
      'Expired' => QrPaymentStatus.expired,
      _ => throw const FormatException('Invalid QR payment status.'),
    };
  }
}

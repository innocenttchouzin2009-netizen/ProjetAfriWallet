import '../../network/api_client.dart';
import '../../network/api_exception.dart';

enum P2PRecipientKind {
  afWalId('afwal-id'),
  qr('qr');

  const P2PRecipientKind(this.wireValue);

  final String wireValue;

  static P2PRecipientKind fromWireValue(String value) {
    return switch (value) {
      'afwal-id' => P2PRecipientKind.afWalId,
      'qr' => P2PRecipientKind.qr,
      _ => throw const FormatException('Invalid P2P recipient kind.'),
    };
  }
}

class P2PTransferRequest {
  const P2PTransferRequest({
    required this.sourceWalletId,
    required this.recipientKind,
    required this.recipientValue,
    required this.currencyCode,
    required this.amountMinor,
    required this.correlationId,
  });

  final String sourceWalletId;
  final P2PRecipientKind recipientKind;
  final String recipientValue;
  final String currencyCode;
  final int amountMinor;
  final String correlationId;

  Map<String, Object?> toJson() => <String, Object?>{
        'sourceWalletId': sourceWalletId,
        'recipientKind': recipientKind.wireValue,
        'recipientValue': recipientValue,
        'currencyCode': currencyCode,
        'amountMinor': amountMinor,
        'correlationId': correlationId,
      };
}

class P2PTransferResponse {
  const P2PTransferResponse({
    required this.transferId,
    required this.sourceWalletId,
    required this.targetWalletId,
    required this.currencyCode,
    required this.amountMinor,
    required this.correlationId,
    required this.createdAtUtc,
    required this.recipientKind,
  });

  final String transferId;
  final String sourceWalletId;
  final String targetWalletId;
  final String currencyCode;
  final int amountMinor;
  final String correlationId;
  final DateTime createdAtUtc;
  final P2PRecipientKind recipientKind;
}

class P2PReceiveIdentityResponse {
  const P2PReceiveIdentityResponse({
    required this.publicLabel,
    required this.qrToken,
  });

  final String publicLabel;
  final String qrToken;
}

class P2PRemoteDataSource {
  const P2PRemoteDataSource(this._apiClient);

  static const String _transfersPath = '/api/v1/p2p/transfers';
  static const String _receiveIdentityPath = '/api/v1/p2p/receive-identity';

  final ApiClient _apiClient;

  Future<P2PTransferResponse> executeTransfer(
    String accessToken,
    P2PTransferRequest request,
  ) async {
    final payload = await _apiClient.postJson(
      _transfersPath,
      body: request.toJson(),
      headers: _bearerHeaders(accessToken),
    );

    try {
      final json = _requireObject(payload);
      return P2PTransferResponse(
        transferId: _requireString(json, 'transferId'),
        sourceWalletId: _requireString(json, 'sourceWalletId'),
        targetWalletId: _requireString(json, 'targetWalletId'),
        currencyCode: _requireString(json, 'currencyCode'),
        amountMinor: _requireInt(json, 'amountMinor'),
        correlationId: _requireString(json, 'correlationId'),
        createdAtUtc: _requireDateTimeUtc(json, 'createdAtUtc'),
        recipientKind: P2PRecipientKind.fromWireValue(
          _requireString(json, 'recipientKind'),
        ),
      );
    } on FormatException {
      throw const ApiMalformedResponseException(
        'The P2P transfer endpoint returned an invalid response.',
      );
    }
  }

  Future<P2PReceiveIdentityResponse> issueReceiveIdentity(
    String accessToken,
  ) async {
    final payload = await _apiClient.postJson(
      _receiveIdentityPath,
      headers: _bearerHeaders(accessToken),
    );

    try {
      final json = _requireObject(payload);
      return P2PReceiveIdentityResponse(
        publicLabel: _requireString(json, 'publicLabel'),
        qrToken: _requireString(json, 'qrToken'),
      );
    } on FormatException {
      throw const ApiMalformedResponseException(
        'The P2P receive identity endpoint returned an invalid response.',
      );
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

  String _requireString(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value is String && value.trim().isNotEmpty) {
      return value;
    }

    throw FormatException('Missing or invalid $key.');
  }

  int _requireInt(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value is int) {
      return value;
    }

    throw FormatException('Missing or invalid $key.');
  }

  DateTime _requireDateTimeUtc(Map<String, dynamic> json, String key) {
    final value = _requireString(json, key);
    final parsed = DateTime.tryParse(value);
    if (parsed == null) {
      throw FormatException('Missing or invalid $key.');
    }

    return parsed.toUtc();
  }
}

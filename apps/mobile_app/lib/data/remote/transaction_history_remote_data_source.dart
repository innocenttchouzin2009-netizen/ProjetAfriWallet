import '../../network/api_client.dart';
import '../../network/api_exception.dart';
import 'transaction_history_contracts.dart';

class TransactionHistoryRemoteDataSource {
  const TransactionHistoryRemoteDataSource(this._apiClient);

  static const String transactionsPath = '/api/v1/transactions';

  final ApiClient _apiClient;

  Future<TransactionHistoryRemotePage> listTransactions(
    String accessToken, {
    int? limit,
    String? cursor,
  }) async {
    final queryParameters = <String, String>{
      if (limit != null) 'limit': limit.toString(),
    };
    if (cursor != null) {
      queryParameters['cursor'] = cursor;
    }

    final payload = await _apiClient.getJson(
      transactionsPath,
      queryParameters: queryParameters,
      headers: <String, String>{
        'Authorization': 'Bearer $accessToken',
      },
    );

    try {
      final json = _requireObject(payload);
      final rawItems = json['items'];
      if (rawItems is! List<dynamic>) {
        throw const FormatException('Invalid transaction history items.');
      }

      final nextCursor = json['nextCursor'];
      if (nextCursor != null &&
          (nextCursor is! String || nextCursor.trim().isEmpty)) {
        throw const FormatException('Invalid transaction history next cursor.');
      }

      return TransactionHistoryRemotePage(
        items: rawItems.map(_parseItem).toList(growable: false),
        nextCursor: nextCursor as String?,
      );
    } on FormatException {
      throw const ApiMalformedResponseException(
        'The transaction history endpoint returned an invalid response.',
      );
    }
  }

  TransactionHistoryRemoteItem _parseItem(Object? value) {
    final json = _requireObject(value);

    final counterpartyLabel = json['counterpartyLabel'];
    if (counterpartyLabel != null && counterpartyLabel is! String) {
      throw const FormatException(
        'Invalid transaction history counterparty label.',
      );
    }

    return TransactionHistoryRemoteItem(
      transactionId: _requireString(json, 'transactionId'),
      walletId: _requireString(json, 'walletId'),
      amountMinor: _requireInt(json, 'amountMinor'),
      currencyCode: _requireString(json, 'currencyCode'),
      direction: TransactionHistoryRemoteDirection.fromWireValue(
        _requireString(json, 'direction'),
      ),
      status: TransactionHistoryRemoteStatus.fromWireValue(
        _requireString(json, 'status'),
      ),
      occurredAtUtc: _requireDateTimeUtc(json, 'occurredAtUtc'),
      reference: _requireString(json, 'reference'),
      counterpartyLabel: counterpartyLabel as String?,
    );
  }

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
    final parsed = DateTime.tryParse(_requireString(json, key));
    if (parsed == null) {
      throw FormatException('Missing or invalid $key.');
    }

    return parsed.toUtc();
  }
}

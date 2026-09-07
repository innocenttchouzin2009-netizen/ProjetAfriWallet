import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import 'api_exception.dart';

class ApiClient {
  ApiClient({
    required this.baseUrl,
    http.Client? httpClient,
    this.timeout = const Duration(seconds: 15),
    Map<String, String>? defaultHeaders,
  }) : _httpClient = httpClient ?? http.Client(),
       _defaultHeaders = <String, String>{
         'Accept': 'application/json',
         ...?defaultHeaders,
       };

  final String baseUrl;
  final Duration timeout;
  final http.Client _httpClient;
  final Map<String, String> _defaultHeaders;

  Future<Object?> getJson(
    String path, {
    Map<String, String>? queryParameters,
    Map<String, String>? headers,
  }) {
    final uri = _buildUri(path, queryParameters: queryParameters);
    return _send(
      () => _httpClient.get(uri, headers: _headers(headers)),
    );
  }

  Future<Object?> postJson(
    String path, {
    Object? body,
    Map<String, String>? queryParameters,
    Map<String, String>? headers,
  }) {
    final uri = _buildUri(path, queryParameters: queryParameters);
    return _send(
      () => _httpClient.post(
        uri,
        headers: _headers(
          <String, String>{
            'Content-Type': 'application/json',
            ...?headers,
          },
        ),
        body: body == null ? null : jsonEncode(body),
      ),
    );
  }

  Uri _buildUri(
    String path, {
    Map<String, String>? queryParameters,
  }) {
    final base = Uri.parse(baseUrl);
    final normalizedBasePath = base.path.endsWith('/')
        ? base.path.substring(0, base.path.length - 1)
        : base.path;
    final normalizedPath = path.startsWith('/') ? path : '/$path';

    return base.replace(
      path: '$normalizedBasePath$normalizedPath',
      queryParameters: queryParameters?.isEmpty ?? true
          ? null
          : queryParameters,
    );
  }

  Map<String, String> _headers(Map<String, String>? headers) {
    return <String, String>{
      ..._defaultHeaders,
      ...?headers,
    };
  }

  Future<Object?> _send(
    Future<http.Response> Function() request,
  ) async {
    late final http.Response response;
    try {
      response = await request().timeout(timeout);
    } on TimeoutException {
      throw const ApiTimeoutException('The request timed out.');
    } on http.ClientException catch (error) {
      throw ApiNetworkException(error.message);
    }

    if (response.statusCode < 200 || response.statusCode >= 300) {
      _throwForStatus(response);
    }

    if (response.bodyBytes.isEmpty) {
      return null;
    }

    try {
      return jsonDecode(utf8.decode(response.bodyBytes));
    } on FormatException {
      throw const ApiMalformedResponseException(
        'The server returned malformed JSON.',
      );
    }
  }

  Never _throwForStatus(http.Response response) {
    final body = response.body.isEmpty ? null : response.body;

    switch (response.statusCode) {
      case 401:
        throw ApiUnauthorizedException(responseBody: body);
      case 403:
        throw ApiForbiddenException(responseBody: body);
      case 404:
        throw ApiNotFoundException(responseBody: body);
      case 409:
        throw ApiConflictException(responseBody: body);
      case 422:
        throw ApiValidationException(responseBody: body);
      default:
        if (response.statusCode >= 500) {
          throw ApiServerException(
            statusCode: response.statusCode,
            responseBody: body,
          );
        }
        throw ApiHttpException(
          'The request failed with HTTP ${response.statusCode}.',
          statusCode: response.statusCode,
          responseBody: body,
        );
    }
  }

  void close() {
    _httpClient.close();
  }
}

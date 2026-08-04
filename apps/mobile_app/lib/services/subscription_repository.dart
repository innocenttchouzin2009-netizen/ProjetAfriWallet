import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/subscription_models.dart';
import 'api_config.dart';

abstract class SubscriptionRepository {
  Future<List<SubscriptionOffer>> fetchOffers({String? country, String? currency, String? query});
  Future<SubscriptionOffer?> fetchOffer(String offerId);
  Future<List<UserSubscription>> fetchUserSubscriptions();
  Future<List<SubscriptionInvoice>> fetchInvoices(String subscriptionId);
  Future<void> createSubscription(String offerId);
  Future<void> cancelSubscription(String subscriptionId);
  Future<void> toggleAutoRenew(String subscriptionId, bool enabled);
}

abstract class SubscriptionApiClient {
  Future<String> get(String url);
  Future<String> post(String url, {String? body});
}

class HttpSubscriptionApiClient implements SubscriptionApiClient {
  HttpSubscriptionApiClient({http.Client? client}) : _client = client ?? http.Client();

  final http.Client _client;

  @override
  Future<String> get(String url) async {
    final response = await _client.get(Uri.parse(url));
    if (response.statusCode >= 400) {
      throw Exception('Request failed: ${response.statusCode} ${response.body}');
    }
    return response.body;
  }

  @override
  Future<String> post(String url, {String? body}) async {
    final response = await _client.post(
      Uri.parse(url),
      headers: {'Content-Type': 'application/json'},
      body: body,
    );
    if (response.statusCode >= 400) {
      throw Exception('Request failed: ${response.statusCode} ${response.body}');
    }
    return response.body;
  }
}

class ApiSubscriptionRepository implements SubscriptionRepository {
  ApiSubscriptionRepository({SubscriptionApiClient? client, String? baseUrl})
      : _client = client ?? HttpSubscriptionApiClient(),
        _baseUrl = baseUrl ?? ApiConfig.baseUrl;

  final SubscriptionApiClient _client;
  final String _baseUrl;

  @override
  Future<List<SubscriptionOffer>> fetchOffers({String? country, String? currency, String? query}) async {
    final queryParameters = <String, String>{};
    if (country != null && country.isNotEmpty) {
      queryParameters['country'] = country;
    }
    if (currency != null && currency.isNotEmpty) {
      queryParameters['currency'] = currency;
    }
    if (query != null && query.isNotEmpty) {
      queryParameters['q'] = query;
    }

    var uri = Uri.parse('$_baseUrl/api/v1/subscriptions/catalog');
    if (queryParameters.isNotEmpty) {
      uri = uri.replace(queryParameters: queryParameters);
    }

    final body = await _client.get(uri.toString());
    final payload = jsonDecode(body) as Map<String, dynamic>;
    final items = payload['items'] as List<dynamic>;

    return items.map((item) => _mapOffer(item as Map<String, dynamic>)).toList();
  }

  @override
  Future<SubscriptionOffer?> fetchOffer(String offerId) async {
    final uri = Uri.parse('$_baseUrl/api/v1/subscriptions/catalog/$offerId');
    final body = await _client.get(uri.toString());
    final payload = jsonDecode(body) as Map<String, dynamic>;
    return _mapOffer(payload);
  }

  @override
  Future<List<UserSubscription>> fetchUserSubscriptions() async {
    final uri = Uri.parse('$_baseUrl/api/v1/subscriptions/lifecycle?userId=user-1');
    final body = await _client.get(uri.toString());
    final payload = jsonDecode(body) as Map<String, dynamic>;
    final items = payload['items'] as List<dynamic>;
    return items.map((item) => _mapUserSubscription(item as Map<String, dynamic>)).toList();
  }

  @override
  Future<List<SubscriptionInvoice>> fetchInvoices(String subscriptionId) async {
    final uri = Uri.parse('$_baseUrl/api/v1/subscriptions/invoices/$subscriptionId');
    final body = await _client.get(uri.toString());
    final payload = jsonDecode(body) as Map<String, dynamic>;
    final items = payload['items'] as List<dynamic>? ?? [payload];
    return items.map((item) => _mapInvoice(item as Map<String, dynamic>)).toList();
  }

  @override
  Future<void> createSubscription(String offerId) async {
    if (offerId.isEmpty) {
      throw Exception('Offer id is required');
    }

    await _client.post(
      '$_baseUrl/api/v1/subscriptions/lifecycle',
      body: jsonEncode({
        'userId': 'user-1',
        'providerId': 'netflix',
        'planId': 'plan-1',
        'offerId': offerId,
        'currency': 'XOF',
        'amountMinor': 0,
        'billingCycle': 'monthly',
        'gracePeriodDays': 7,
      }),
    );
  }

  @override
  Future<void> cancelSubscription(String subscriptionId) async {
    if (subscriptionId.isEmpty) {
      throw Exception('Subscription id is required');
    }

    await _client.post('$_baseUrl/api/v1/subscriptions/lifecycle/$subscriptionId/cancel');
  }

  @override
  Future<void> toggleAutoRenew(String subscriptionId, bool enabled) async {
    if (subscriptionId.isEmpty) {
      throw Exception('Subscription id is required');
    }

    if (enabled) {
      await _client.post('$_baseUrl/api/v1/subscriptions/lifecycle/$subscriptionId/renew');
    } else {
      await _client.post('$_baseUrl/api/v1/subscriptions/lifecycle/$subscriptionId/cancel');
    }
  }

  SubscriptionOffer _mapOffer(Map<String, dynamic> item) {
    final priceMinor = item['priceMinor'] as num? ?? 0;
    final features = <String>[];
    if (item['features'] is List) {
      for (final feature in item['features'] as List) {
        features.add(feature.toString());
      }
    }

    return SubscriptionOffer(
      id: item['id']?.toString() ?? item['offerId']?.toString() ?? '',
      providerId: item['providerId']?.toString() ?? '',
      name: item['name']?.toString() ?? '',
      description: item['description']?.toString() ?? '',
      price: priceMinor / 100,
      currency: item['currency']?.toString() ?? 'XOF',
      country: item['country']?.toString() ?? '',
      category: item['category']?.toString() ?? '',
      features: features,
      longDescription: item['description']?.toString() ?? '',
    );
  }

  UserSubscription _mapUserSubscription(Map<String, dynamic> item) {
    final amountMinor = item['amountMinor'] as num? ?? 0;
    return UserSubscription(
      id: item['subscriptionId']?.toString() ?? '',
      offerId: item['offerId']?.toString() ?? '',
      providerId: item['providerId']?.toString() ?? '',
      name: item['providerId']?.toString() ?? '',
      status: item['status']?.toString() ?? 'Unknown',
      autoRenew: true,
      nextBillingDate: item['renewalAt']?.toString() ?? '',
      currentCycle: 'Cycle 1',
      price: amountMinor / 100,
      currency: item['currency']?.toString() ?? 'XOF',
    );
  }

  SubscriptionInvoice _mapInvoice(Map<String, dynamic> item) {
    final amountMinor = item['amountMinor'] as num? ?? 0;
    return SubscriptionInvoice(
      id: item['invoiceId']?.toString() ?? '',
      subscriptionId: item['subscriptionId']?.toString() ?? '',
      amount: amountMinor / 100,
      currency: item['currency']?.toString() ?? 'XOF',
      status: item['status']?.toString() ?? 'Unknown',
      issueDate: item['createdAt']?.toString() ?? '',
    );
  }
}

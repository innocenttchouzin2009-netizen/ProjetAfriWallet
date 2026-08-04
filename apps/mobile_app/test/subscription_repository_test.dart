import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/services/subscription_repository.dart';

void main() {
  group('ApiSubscriptionRepository', () {
    test('maps catalog payload to offers', () async {
      final repository = ApiSubscriptionRepository(
        baseUrl: 'https://localhost:5001',
        client: _FakeClient(
          responses: {
            'https://localhost:5001/api/v1/subscriptions/catalog': '{"items":[{"id":"offer-1","providerId":"netflix","name":"Netflix Standard","description":"Streaming","category":"VideoStreaming","country":"CM","currency":"XOF","priceMinor":8990,"billingCycle":"monthly","isFeatured":true,"isAvailable":true,"validFrom":"2024-01-01T00:00:00Z","validTo":null,"promotionCode":"SAVE10","discountPercent":10,"isNew":false,"createdAt":"2024-01-01T00:00:00Z","updatedAt":"2024-01-01T00:00:00Z"}],"page":1,"pageSize":20,"total":1}',
            'https://localhost:5001/api/v1/subscriptions/lifecycle?userId=user-1': '{"items":[{"subscriptionId":"sub-1","userId":"user-1","providerId":"netflix","planId":"plan-1","offerId":"offer-1","currency":"XOF","amountMinor":8990,"billingCycle":"monthly","gracePeriodDays":7,"status":"Active","createdAt":"2024-01-01T00:00:00Z","updatedAt":"2024-01-01T00:00:00Z","startedAt":"2024-01-02T00:00:00Z","endedAt":null,"renewalAt":"2024-02-01T00:00:00Z","lastPaymentAt":"2024-01-02T00:00:00Z","history":["Active:created"]}]}',
          },
        ),
      );

      final offers = await repository.fetchOffers();
      expect(offers.single.name, 'Netflix Standard');
      expect(offers.single.price, 89.9);
    });
  });
}

class _FakeClient implements SubscriptionApiClient {
  _FakeClient({required this.responses});

  final Map<String, String> responses;

  @override
  Future<String> get(String url) async {
    final response = responses[url];
    if (response == null) {
      throw Exception('Unexpected URL: $url');
    }
    return response;
  }

  @override
  Future<String> post(String url, {String? body}) async {
    return '{}';
  }
}

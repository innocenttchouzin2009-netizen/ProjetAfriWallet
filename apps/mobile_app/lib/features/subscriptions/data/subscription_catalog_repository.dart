import '../domain/subscription_provider.dart';

abstract interface class SubscriptionCatalogRepository {
  Future<List<SubscriptionProvider>> getProviders();
}

class DemoSubscriptionCatalogRepository implements SubscriptionCatalogRepository {
  @override
  Future<List<SubscriptionProvider>> getProviders() async {
    await Future<void>.delayed(const Duration(milliseconds: 350));

    return const <SubscriptionProvider>[
      SubscriptionProvider(
        id: 'netflix',
        name: 'Netflix',
        category: SubscriptionCategory.streaming,
        status: SubscriptionProviderStatus.comingSoon,
        description: 'Films, séries et documentaires.',
        iconText: 'N',
      ),
      SubscriptionProvider(
        id: 'canal-plus',
        name: 'Canal+',
        category: SubscriptionCategory.television,
        status: SubscriptionProviderStatus.comingSoon,
        description: 'Télévision, sport, cinéma et divertissement.',
        iconText: 'C+',
      ),
      SubscriptionProvider(
        id: 'my-bouquet-africain',
        name: 'MyBouquetAfricain',
        category: SubscriptionCategory.television,
        status: SubscriptionProviderStatus.comingSoon,
        description: 'Chaînes et contenus destinés à la diaspora africaine.',
        iconText: 'MBA',
      ),
      SubscriptionProvider(
        id: 'cinaf',
        name: 'Cinaf',
        category: SubscriptionCategory.streaming,
        status: SubscriptionProviderStatus.comingSoon,
        description: 'Cinéma et contenus audiovisuels africains.',
        iconText: 'C',
      ),
    ];
  }
}

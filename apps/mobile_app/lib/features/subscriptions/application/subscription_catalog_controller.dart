import 'package:flutter/foundation.dart';

import '../data/subscription_catalog_repository.dart';
import '../domain/subscription_provider.dart';

enum SubscriptionCatalogStatus {
  initial,
  loading,
  success,
  error,
}

class SubscriptionCatalogController extends ChangeNotifier {
  SubscriptionCatalogController({
    required SubscriptionCatalogRepository repository,
  }) : _repository = repository;

  final SubscriptionCatalogRepository _repository;

  SubscriptionCatalogStatus _status = SubscriptionCatalogStatus.initial;
  List<SubscriptionProvider> _providers = const <SubscriptionProvider>[];
  String? _errorMessage;

  SubscriptionCatalogStatus get status => _status;
  List<SubscriptionProvider> get providers => List<SubscriptionProvider>.unmodifiable(_providers);
  String? get errorMessage => _errorMessage;

  Future<void> load() async {
    if (_status == SubscriptionCatalogStatus.loading) {
      return;
    }

    _status = SubscriptionCatalogStatus.loading;
    _errorMessage = null;
    notifyListeners();

    try {
      _providers = await _repository.getProviders();
      _status = SubscriptionCatalogStatus.success;
    } catch (_) {
      _errorMessage = 'Impossible de charger les abonnements.';
      _status = SubscriptionCatalogStatus.error;
    }

    notifyListeners();
  }
}

enum SubscriptionCategory {
  streaming,
  television,
  music,
  internet,
  software,
  education,
  other,
}

enum SubscriptionProviderStatus {
  available,
  comingSoon,
  unavailable,
}

class SubscriptionPlan {
  const SubscriptionPlan({
    required this.id,
    required this.name,
    required this.amountMinor,
    required this.currencyCode,
    required this.billingCycle,
  });

  final String id;
  final String name;
  final int amountMinor;
  final String currencyCode;
  final String billingCycle;
}

class SubscriptionProvider {
  const SubscriptionProvider({
    required this.id,
    required this.name,
    required this.category,
    required this.status,
    required this.description,
    required this.iconText,
    this.plans = const <SubscriptionPlan>[],
  });

  final String id;
  final String name;
  final SubscriptionCategory category;
  final SubscriptionProviderStatus status;
  final String description;
  final String iconText;
  final List<SubscriptionPlan> plans;

  bool get isAvailable => status == SubscriptionProviderStatus.available;
}

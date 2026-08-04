class SubscriptionOffer {
  const SubscriptionOffer({
    required this.id,
    required this.providerId,
    required this.name,
    required this.description,
    required this.price,
    required this.currency,
    required this.country,
    required this.category,
    required this.features,
    required this.longDescription,
  });

  final String id;
  final String providerId;
  final String name;
  final String description;
  final double price;
  final String currency;
  final String country;
  final String category;
  final List<String> features;
  final String longDescription;
}

class UserSubscription {
  const UserSubscription({
    required this.id,
    required this.offerId,
    required this.providerId,
    required this.name,
    required this.status,
    required this.autoRenew,
    required this.nextBillingDate,
    required this.currentCycle,
    required this.price,
    required this.currency,
  });

  final String id;
  final String offerId;
  final String providerId;
  final String name;
  final String status;
  final bool autoRenew;
  final String nextBillingDate;
  final String currentCycle;
  final double price;
  final String currency;
}

class SubscriptionInvoice {
  const SubscriptionInvoice({
    required this.id,
    required this.subscriptionId,
    required this.amount,
    required this.currency,
    required this.status,
    required this.issueDate,
  });

  final String id;
  final String subscriptionId;
  final double amount;
  final String currency;
  final String status;
  final String issueDate;
}

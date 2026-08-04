import 'package:flutter/material.dart';

import 'pages/subscriptions_page.dart';
import 'services/subscription_repository.dart';

void main() {
  runApp(const AfriWalletApp());
}

class AfriWalletApp extends StatelessWidget {
  const AfriWalletApp({super.key, this.repository});

  final SubscriptionRepository? repository;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'AfriWallet',
      theme: ThemeData(useMaterial3: true, colorSchemeSeed: Colors.indigo),
      home: SubscriptionsPage(repository: repository),
    );
  }
}

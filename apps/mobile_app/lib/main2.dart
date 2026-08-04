import 'package:flutter/material.dart';

import 'pages/subscriptions_page.dart';

void main() {
  runApp(const AfriWalletApp());
}

class AfriWalletApp extends StatelessWidget {
  const AfriWalletApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'AfriWallet',
      theme: ThemeData(useMaterial3: true, colorSchemeSeed: Colors.indigo),
      home: const SubscriptionsPage(),
    );
  }
}

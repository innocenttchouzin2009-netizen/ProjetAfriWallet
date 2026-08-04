import 'package:flutter/material.dart';
import '../fx/fx_quote_screen.dart';
import 'wallet_create_screen.dart';
import 'wallet_controller.dart';
import 'wallet_detail_screen.dart';
import 'wallet_screen.dart';

class WalletRouter {
  static Route<dynamic> onGenerateRoute(RouteSettings settings) {
    switch (settings.name) {
      case '/wallets':
        return MaterialPageRoute(builder: (_) => const WalletScreen());
      case '/wallets/create':
        return MaterialPageRoute(
          builder: (_) => WalletCreateScreen(controller: WalletController()),
        );
      case '/wallets/:walletId':
        final walletId = settings.arguments as String?;
        return MaterialPageRoute(
          builder: (_) => WalletDetailScreen(walletId: walletId ?? ''),
        );
      case '/fx/quote':
        return MaterialPageRoute(builder: (_) => const FxQuoteScreen());
      default:
        return MaterialPageRoute(builder: (_) => const WalletScreen());
    }
  }
}

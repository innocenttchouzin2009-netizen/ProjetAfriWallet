import 'package:go_router/go_router.dart';

import '../../features/financial_desk/presentation/screens/financial_desk_screen.dart';
import '../../features/onboarding/presentation/screens/splash_screen.dart';
import '../../features/onboarding/presentation/screens/welcome_screen.dart';

abstract final class AppRoutes {
  static const String splash = '/';
  static const String welcome = '/welcome';
  static const String financialDesk = '/financial-desk';
}

final GoRouter appRouter = GoRouter(
  initialLocation: AppRoutes.splash,
  routes: [
    GoRoute(
      path: AppRoutes.splash,
      builder: (context, state) => const SplashScreen(),
    ),
    GoRoute(
      path: AppRoutes.welcome,
      builder: (context, state) => const WelcomeScreen(),
    ),
    GoRoute(
      path: AppRoutes.financialDesk,
      builder: (context, state) => const FinancialDeskScreen(),
    ),
  ],
);

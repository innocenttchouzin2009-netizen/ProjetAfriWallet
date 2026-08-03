import 'package:flutter/material.dart';

enum CardMode { personal, business, association }

class AfriCardScreen extends StatefulWidget {
  const AfriCardScreen({super.key});

  @override
  State<AfriCardScreen> createState() => _AfriCardScreenState();
}

class _AfriCardScreenState extends State<AfriCardScreen>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller;
  late final Animation<Offset> _cardSlide;
  late final Animation<double> _cardFade;
  late final Animation<double> _qrFade;
  late final Animation<double> _badgesFade;
  late final Animation<double> _actionsFade;
  CardMode _mode = CardMode.personal;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1400),
    )..forward();
    _cardSlide = Tween(begin: const Offset(0, 0.18), end: Offset.zero).animate(
      CurvedAnimation(parent: _controller, curve: const Interval(0.0, 0.45, curve: Curves.easeOutCubic)),
    );
    _cardFade = CurvedAnimation(parent: _controller, curve: const Interval(0.0, 0.45));
    _qrFade = CurvedAnimation(parent: _controller, curve: const Interval(0.35, 0.65));
    _badgesFade = CurvedAnimation(parent: _controller, curve: const Interval(0.55, 0.8));
    _actionsFade = CurvedAnimation(parent: _controller, curve: const Interval(0.75, 1.0));
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final model = _cardModel(_mode);

    return Scaffold(
      appBar: AppBar(title: const Text('AfriCard')),
      body: Container(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [Color(0xFFF7F1DF), Color(0xFFE8F3EE), Color(0xFFFDF9F2)],
          ),
        ),
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            SegmentedButton<CardMode>(
              segments: const [
                ButtonSegment(value: CardMode.personal, icon: Icon(Icons.person_outline), label: Text('Personnel')),
                ButtonSegment(value: CardMode.business, icon: Icon(Icons.storefront_outlined), label: Text('Business')),
                ButtonSegment(value: CardMode.association, icon: Icon(Icons.groups_outlined), label: Text('Association')),
              ],
              selected: {_mode},
              onSelectionChanged: (selection) {
                setState(() => _mode = selection.first);
                _controller.forward(from: 0.35);
              },
            ),
            const SizedBox(height: 18),
            SlideTransition(
              position: _cardSlide,
              child: FadeTransition(
                opacity: _cardFade,
                child: _IdentityCard(model: model, qrFade: _qrFade, badgesFade: _badgesFade),
              ),
            ),
            const SizedBox(height: 20),
            FadeTransition(
              opacity: _actionsFade,
              child: Wrap(
                spacing: 10,
                runSpacing: 10,
                children: const [
                  _QuickAction(icon: Icons.ios_share, label: 'Partager'),
                  _QuickAction(icon: Icons.qr_code_scanner_rounded, label: 'Scanner'),
                  _QuickAction(icon: Icons.download_rounded, label: 'Telecharger'),
                  _QuickAction(icon: Icons.qr_code_2_rounded, label: 'Afficher QR'),
                  _QuickAction(icon: Icons.point_of_sale_rounded, label: 'Creer QR paiement'),
                  _QuickAction(icon: Icons.timer_outlined, label: 'Creer QR temporaire'),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  _CardModel _cardModel(CardMode mode) {
    switch (mode) {
      case CardMode.personal:
        return const _CardModel(
          heading: 'AfriWallet',
          status: 'Verified Identity',
          alias: '@innocent',
          awid: 'AW-237-K9M4X2Q8',
          details: null,
          badges: ['Verified'],
        );
      case CardMode.business:
        return const _CardModel(
          heading: 'AfriBusiness',
          status: 'Merchant Profile',
          alias: '@innocent_shop',
          awid: 'AW-237-K9M4X2Q8',
          details: 'Afri Merchant • 08:00-18:00',
          badges: ['Verified', 'Merchant'],
        );
      case CardMode.association:
        return const _CardModel(
          heading: 'AfriCircle',
          status: 'Association Role',
          alias: '@innocent_circle',
          awid: 'AW-237-K9M4X2Q8',
          details: 'Treasurer • Unity Association',
          badges: ['Verified', 'Association'],
        );
    }
  }
}

class _IdentityCard extends StatelessWidget {
  const _IdentityCard({required this.model, required this.qrFade, required this.badgesFade});

  final _CardModel model;
  final Animation<double> qrFade;
  final Animation<double> badgesFade;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(24),
        gradient: const LinearGradient(
          colors: [Color(0xFF0A473B), Color(0xFF0C6350), Color(0xFF103D35)],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        boxShadow: const [
          BoxShadow(
            color: Color(0x33000000),
            blurRadius: 24,
            offset: Offset(0, 14),
          ),
        ],
      ),
      padding: const EdgeInsets.all(22),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(model.heading, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w700, fontSize: 22)),
          const SizedBox(height: 6),
          Text(model.status, style: const TextStyle(color: Color(0xFFD6EBDC), fontWeight: FontWeight.w600)),
          const SizedBox(height: 16),
          Text(model.alias, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w700, fontSize: 20)),
          const SizedBox(height: 4),
          Text(model.awid, style: const TextStyle(color: Color(0xFFD6EBDC), letterSpacing: 0.8)),
          if (model.details != null) ...[
            const SizedBox(height: 6),
            Text(model.details!, style: const TextStyle(color: Color(0xFFC5E3D6))),
          ],
          const SizedBox(height: 18),
          FadeTransition(
            opacity: qrFade,
            child: const _QrPreview(),
          ),
          const SizedBox(height: 14),
          const Text('Secure QR Identity', style: TextStyle(color: Colors.white70, fontWeight: FontWeight.w600)),
          const SizedBox(height: 10),
          FadeTransition(
            opacity: badgesFade,
            child: Wrap(
              spacing: 8,
              runSpacing: 8,
              children: model.badges
                  .map((badge) => Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                        decoration: BoxDecoration(
                          color: const Color(0x22FFFFFF),
                          borderRadius: BorderRadius.circular(999),
                          border: Border.all(color: const Color(0x44FFFFFF)),
                        ),
                        child: Text(badge, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600, fontSize: 12)),
                      ))
                  .toList(),
            ),
          ),
        ],
      ),
    );
  }
}

class _QrPreview extends StatelessWidget {
  const _QrPreview();

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 118,
      width: double.infinity,
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
      ),
      child: GridView.builder(
        physics: const NeverScrollableScrollPhysics(),
        padding: const EdgeInsets.all(12),
        gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 12),
        itemCount: 144,
        itemBuilder: (context, index) {
          final row = index ~/ 12;
          final col = index % 12;
          final dot = (row + col) % 3 == 0 || row == col || row + col == 11;
          return Container(
            margin: const EdgeInsets.all(1.2),
            decoration: BoxDecoration(
              color: dot ? const Color(0xFF111111) : Colors.transparent,
              borderRadius: BorderRadius.circular(1.2),
            ),
          );
        },
      ),
    );
  }
}

class _QuickAction extends StatelessWidget {
  const _QuickAction({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 168,
      child: FilledButton.tonalIcon(
        onPressed: () {},
        icon: Icon(icon),
        label: Text(label),
      ),
    );
  }
}

class _CardModel {
  const _CardModel({
    required this.heading,
    required this.status,
    required this.alias,
    required this.awid,
    required this.details,
    required this.badges,
  });

  final String heading;
  final String status;
  final String alias;
  final String awid;
  final String? details;
  final List<String> badges;
}

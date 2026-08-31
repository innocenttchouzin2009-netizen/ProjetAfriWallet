import 'package:flutter/material.dart';

import '../theme/afwal_theme.dart';

class AfWalBrandMark extends StatelessWidget {
  const AfWalBrandMark({super.key, this.size = 48, this.showWordmark = true});

  final double size;
  final bool showWordmark;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: size,
          height: size,
          decoration: BoxDecoration(
            color: AfWalColors.deepGreen,
            borderRadius: BorderRadius.circular(size * .33),
          ),
          alignment: Alignment.center,
          child: Text(
            'AW',
            style: TextStyle(
              color: AfWalColors.gold,
              fontSize: size * .32,
              fontWeight: FontWeight.w900,
              letterSpacing: -.5,
            ),
          ),
        ),
        if (showWordmark) ...[
          const SizedBox(width: 12),
          Text('AfWal', style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800)),
        ],
      ],
    );
  }
}

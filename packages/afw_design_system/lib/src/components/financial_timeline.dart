import 'package:flutter/material.dart';

class FinancialTimelineEvent {
  const FinancialTimelineEvent({
    required this.title,
    required this.amount,
    required this.timeLabel,
    required this.currency,
    required this.isCredit,
    this.icon = Icons.payments_outlined,
  });

  final String title;
  final double amount;
  final String timeLabel;
  final String currency;
  final bool isCredit;
  final IconData icon;
}

class FinancialTimeline extends StatelessWidget {
  const FinancialTimeline({
    super.key,
    required this.sectionTitle,
    required this.events,
  });

  final String sectionTitle;
  final List<FinancialTimelineEvent> events;

  @override
  Widget build(BuildContext context) {
    if (events.isEmpty) {
      return const Center(
        child: Text('Aucune operation pour le moment.'),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          sectionTitle,
          style: Theme.of(context).textTheme.titleLarge,
        ),
        const SizedBox(height: 16),
        ...events.map((event) => _TimelineRow(event: event)),
      ],
    );
  }
}

class _TimelineRow extends StatelessWidget {
  const _TimelineRow({required this.event});

  final FinancialTimelineEvent event;

  @override
  Widget build(BuildContext context) {
    final amountPrefix = event.isCredit ? '+' : '-';
    final amountColor = event.isCredit ? const Color(0xFF0C8F5A) : const Color(0xFFB02A37);

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFFE7ECEA)),
      ),
      child: Row(
        children: [
          Container(
            width: 40,
            height: 40,
            decoration: BoxDecoration(
              color: const Color(0xFFF2F7F5),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(event.icon, color: const Color(0xFF1F8A70)),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  event.title,
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                const SizedBox(height: 2),
                Text(
                  event.timeLabel,
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ],
            ),
          ),
          Text(
            '$amountPrefix${event.amount.toStringAsFixed(0)} ${event.currency}',
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  color: amountColor,
                  fontWeight: FontWeight.w700,
                ),
          ),
        ],
      ),
    );
  }
}

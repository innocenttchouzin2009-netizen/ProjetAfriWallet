class AwidProfile {
  const AwidProfile({
    required this.awid,
    required this.displayName,
    required this.isPrivate,
    this.alias,
  });

  final String awid;
  final String displayName;
  final bool isPrivate;
  final String? alias;

  String get publicLabel => alias?.trim().isNotEmpty == true ? alias! : awid;
}

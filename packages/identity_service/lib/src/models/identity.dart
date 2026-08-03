class Identity {
  const Identity({
    required this.userId,
    required this.firstName,
    required this.lastName,
    required this.birthDate,
    required this.country,
    required this.residence,
    required this.language,
  });

  final String userId;
  final String firstName;
  final String lastName;
  final DateTime birthDate;
  final String country;
  final String residence;
  final String language;
}

class ApiException implements Exception {
  const ApiException(this.message);

  final String message;

  @override
  String toString() => message;
}

class ApiNetworkException extends ApiException {
  const ApiNetworkException(super.message);
}

class ApiTimeoutException extends ApiException {
  const ApiTimeoutException(super.message);
}

class ApiHttpException extends ApiException {
  const ApiHttpException(
    super.message, {
    required this.statusCode,
    this.responseBody,
  });

  final int statusCode;
  final String? responseBody;
}

class ApiUnauthorizedException extends ApiHttpException {
  const ApiUnauthorizedException({super.responseBody})
    : super('Authentication is required.', statusCode: 401);
}

class ApiForbiddenException extends ApiHttpException {
  const ApiForbiddenException({super.responseBody})
    : super('Access is forbidden.', statusCode: 403);
}

class ApiNotFoundException extends ApiHttpException {
  const ApiNotFoundException({super.responseBody})
    : super('The requested resource was not found.', statusCode: 404);
}

class ApiConflictException extends ApiHttpException {
  const ApiConflictException({super.responseBody})
    : super(
        'The request conflicts with the current resource state.',
        statusCode: 409,
      );
}

class ApiValidationException extends ApiHttpException {
  const ApiValidationException({super.responseBody})
    : super('The request could not be validated.', statusCode: 422);
}

class ApiServerException extends ApiHttpException {
  const ApiServerException({
    required super.statusCode,
    super.responseBody,
  }) : super('The server could not complete the request.');
}

class ApiMalformedResponseException extends ApiException {
  const ApiMalformedResponseException(super.message);
}

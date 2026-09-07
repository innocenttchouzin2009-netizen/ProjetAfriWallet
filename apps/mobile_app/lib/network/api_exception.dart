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
  const ApiUnauthorizedException({String? responseBody})
    : super(
        'Authentication is required.',
        statusCode: 401,
        responseBody: responseBody,
      );
}

class ApiForbiddenException extends ApiHttpException {
  const ApiForbiddenException({String? responseBody})
    : super(
        'Access is forbidden.',
        statusCode: 403,
        responseBody: responseBody,
      );
}

class ApiNotFoundException extends ApiHttpException {
  const ApiNotFoundException({String? responseBody})
    : super(
        'The requested resource was not found.',
        statusCode: 404,
        responseBody: responseBody,
      );
}

class ApiConflictException extends ApiHttpException {
  const ApiConflictException({String? responseBody})
    : super(
        'The request conflicts with the current resource state.',
        statusCode: 409,
        responseBody: responseBody,
      );
}

class ApiValidationException extends ApiHttpException {
  const ApiValidationException({String? responseBody})
    : super(
        'The request could not be validated.',
        statusCode: 422,
        responseBody: responseBody,
      );
}

class ApiServerException extends ApiHttpException {
  const ApiServerException({
    required int statusCode,
    String? responseBody,
  }) : super(
         'The server could not complete the request.',
         statusCode: statusCode,
         responseBody: responseBody,
       );
}

class ApiMalformedResponseException extends ApiException {
  const ApiMalformedResponseException(super.message);
}

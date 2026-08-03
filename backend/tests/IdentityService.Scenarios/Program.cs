using IdentityService.Api.Engine;

try
{
	RunScenarios();
	Console.WriteLine("All DLV-0003.8 scenarios passed.");
	return 0;
}
catch (Exception ex)
{
	Console.Error.WriteLine($"Scenario failure: {ex.Message}");
	return 1;
}

static void RunScenarios()
{
	var repository = new InMemoryIdentityRepository();
	var qrService = new QrTokenService();
	var privacyResolver = new PrivacyResolver();
	var cardService = new IdentityCardService();

	var account = repository.GetOrCreateAccount("demo-user");

	// 1) QR valide
	var validQr = qrService.CreateSignedToken(account, QrType.Identity, "IDENTITY_SHARE", null, int.MaxValue, null, null);
	repository.CreateQrToken(validQr);
	var validResult = qrService.Resolve(validQr.Token, QrType.Identity, repository, privacyResolver);
	Ensure(validResult.Success, "Expected valid QR to resolve");

	// 2) QR expire
	var expiredQr = qrService.CreateSignedToken(account, QrType.PaymentRequest, "RECEIVE_PAYMENT", DateTimeOffset.UtcNow.AddMinutes(-1), 1, 10m, "EUR");
	repository.CreateQrToken(expiredQr);
	var expiredResult = qrService.Resolve(expiredQr.Token, QrType.PaymentRequest, repository, privacyResolver);
	Ensure(!expiredResult.Success && expiredResult.ErrorCode == "QR_TOKEN_EXPIRED", "Expected expired QR failure");

	// 3) QR revoque
	var revokedQr = qrService.CreateSignedToken(account, QrType.Contact, "CONTACT", DateTimeOffset.UtcNow.AddMinutes(10), 3, null, null);
	repository.CreateQrToken(revokedQr);
	repository.RevokeQrToken(revokedQr.Id, account.SubjectId);
	var revokedResult = qrService.Resolve(revokedQr.Token, QrType.Contact, repository, privacyResolver);
	Ensure(!revokedResult.Success && revokedResult.ErrorCode == "QR_TOKEN_REVOKED", "Expected revoked QR failure");

	// 4) Signature invalide
	var tampered = validQr.Token[..^1] + (validQr.Token.EndsWith('A') ? "B" : "A");
	var tamperedResult = qrService.Resolve(tampered, QrType.Identity, repository, privacyResolver);
	Ensure(!tamperedResult.Success && tamperedResult.ErrorCode == "QR_SIGNATURE_INVALID", "Expected invalid signature failure");

	// 5) Mauvais type
	var typeMismatch = qrService.Resolve(validQr.Token, QrType.Payment, repository, privacyResolver);
	Ensure(!typeMismatch.Success && typeMismatch.ErrorCode == "QR_PURPOSE_INVALID", "Expected wrong type failure");

	// 6) QR paiement deja utilise
	var paymentQr = qrService.CreateSignedToken(account, QrType.Payment, "RECEIVE_PAYMENT", DateTimeOffset.UtcNow.AddMinutes(5), 1, 25m, "EUR");
	repository.CreateQrToken(paymentQr);
	var firstPayment = qrService.Resolve(paymentQr.Token, QrType.Payment, repository, privacyResolver);
	var secondPayment = qrService.Resolve(paymentQr.Token, QrType.Payment, repository, privacyResolver);
	Ensure(firstPayment.Success, "Expected first payment scan success");
	Ensure(!secondPayment.Success && secondPayment.ErrorCode == "QR_PAYMENT_ALREADY_USED", "Expected payment reuse failure");

	// 7) QR temporaire expire
	var tempQr = qrService.CreateSignedToken(account, QrType.Contact, "TEMP_CONTACT", DateTimeOffset.UtcNow.AddSeconds(-1), 1, null, null);
	repository.CreateQrToken(tempQr);
	var tempExpired = qrService.Resolve(tempQr.Token, QrType.Contact, repository, privacyResolver);
	Ensure(!tempExpired.Success && tempExpired.ErrorCode == "QR_TOKEN_EXPIRED", "Expected temporary QR expiry failure");

	// 8) Resolution avec confidentialite
	account.PrivacyMode = PrivacyMode.Private;
	var privatePreview = qrService.Resolve(validQr.Token, QrType.Identity, repository, privacyResolver);
	Ensure(privatePreview.Success && privatePreview.Recipient is not null, "Expected private preview success");
	Ensure(string.IsNullOrWhiteSpace(privatePreview.Recipient?.DisplayName), "Private mode must hide displayName");

	account.PrivacyMode = PrivacyMode.Standard;
	var standardPreview = qrService.Resolve(validQr.Token, QrType.Identity, repository, privacyResolver);
	Ensure(standardPreview.Success && !string.IsNullOrWhiteSpace(standardPreview.Recipient?.DisplayName), "Standard mode should expose displayName");

	// 9) Carte numerique multi-contextes
	var personal = cardService.BuildCard(account, IdentityCardContext.Personal);
	var business = cardService.BuildCard(account, IdentityCardContext.Business);
	var association = cardService.BuildCard(account, IdentityCardContext.Association);

	Ensure(personal.Context == IdentityCardContext.Personal, "Personal card context mismatch");
	Ensure(!string.IsNullOrWhiteSpace(business.BusinessName), "Business card should include business info");
	Ensure(!string.IsNullOrWhiteSpace(association.AssociationName), "Association card should include association info");
}

static void Ensure(bool condition, string message)
{
	if (!condition)
	{
		throw new InvalidOperationException(message);
	}
}

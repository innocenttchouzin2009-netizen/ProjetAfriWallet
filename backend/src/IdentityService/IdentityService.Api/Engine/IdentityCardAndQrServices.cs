using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IdentityService.Api.Engine;

public sealed class IdentityCardService
{
    public DigitalIdentityCard BuildCard(IdentityAccount account, IdentityCardContext context)
    {
        var badges = new List<string>(account.VerificationBadges);
        if (context == IdentityCardContext.Business && !badges.Contains("MERCHANT"))
        {
            badges.Add("MERCHANT");
        }

        if (context == IdentityCardContext.Association && !badges.Contains("ASSOCIATION"))
        {
            badges.Add("ASSOCIATION");
        }

        return new DigitalIdentityCard
        {
            AwidId = account.AwidId,
            Alias = account.Alias,
            PublicAwid = account.PublicAwid,
            DisplayName = account.DisplayName,
            ProfilePhoto = account.ProfilePhoto,
            VerificationBadges = badges,
            PrimaryWallet = account.PrimaryWallet,
            PreferredCurrency = account.PreferredCurrency,
            Theme = account.Theme,
            PrivacyMode = account.PrivacyMode,
            Context = context,
            BusinessName = context == IdentityCardContext.Business ? account.BusinessName : null,
            AssociationName = context == IdentityCardContext.Association ? account.AssociationName : null,
            BusinessHours = context == IdentityCardContext.Business ? account.BusinessHours : null,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}

public sealed class PrivacyResolver
{
    public RecipientPreview BuildPreview(IdentityAccount account)
    {
        var basePreview = new RecipientPreview
        {
            RecipientId = $"rcp_{Guid.NewGuid():N}"[..20],
            Alias = account.Alias,
            PublicAwid = account.PublicAwid,
            AvatarUrl = account.ProfilePhoto,
            PrivacyMode = account.PrivacyMode.ToString().ToUpperInvariant()
        };

        if (account.PrivacyMode == PrivacyMode.Private)
        {
            return basePreview;
        }

        return new RecipientPreview
        {
            RecipientId = basePreview.RecipientId,
            Alias = basePreview.Alias,
            PublicAwid = basePreview.PublicAwid,
            AvatarUrl = basePreview.AvatarUrl,
            PrivacyMode = basePreview.PrivacyMode,
            DisplayName = account.PrivacyMode == PrivacyMode.Professional ? account.BusinessName ?? account.DisplayName : account.DisplayName,
            Country = account.Country,
            VerificationBadges = account.VerificationBadges
        };
    }
}

public sealed class QrTokenService
{
    private readonly byte[] _signingKey;

    public QrTokenService()
    {
        _signingKey = Encoding.UTF8.GetBytes("afriwallet-qr-signing-key-v0-2-8");
    }

    public QrToken CreateSignedToken(IdentityAccount account, QrType type, string purpose, DateTimeOffset? expiresAt, int maxUses, decimal? amount, string? currency)
    {
        var qr = new QrToken
        {
            AwidId = account.AwidId,
            SubjectId = account.SubjectId,
            Type = type,
            Purpose = purpose,
            ExpiresAt = expiresAt,
            MaxUses = Math.Max(1, maxUses),
            Amount = amount,
            Currency = currency
        };

        qr.Token = BuildSignedToken(qr);
        return qr;
    }

    public QrResolveResult Resolve(string token, QrType? expectedType, IIdentityRepository repository, PrivacyResolver privacyResolver)
    {
        if (!TryParseToken(token, out var payload, out var error))
        {
            return new QrResolveResult { Success = false, ErrorCode = error, Message = "Invalid token" };
        }

        if (payload is null)
        {
            return new QrResolveResult { Success = false, ErrorCode = "QR_SIGNATURE_INVALID", Message = "Invalid token" };
        }

        if (expectedType is not null && payload.Type != expectedType.Value)
        {
            return new QrResolveResult { Success = false, ErrorCode = "QR_PURPOSE_INVALID", Message = "Wrong QR type" };
        }

        var qr = repository.GetQrTokenById(payload.Id);
        if (qr is null)
        {
            return new QrResolveResult { Success = false, ErrorCode = "QR_NOT_FOUND", Message = "QR not found" };
        }

        if (qr.RevokedAt is not null)
        {
            return new QrResolveResult { Success = false, ErrorCode = "QR_TOKEN_REVOKED", Message = "QR revoked" };
        }

        if (qr.ExpiresAt is not null && qr.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            repository.AddAudit(new AuditEvent { EventType = "QR_EXPIRED", SubjectId = qr.SubjectId, QrId = qr.Id, Details = "Token expired" });
            return new QrResolveResult { Success = false, ErrorCode = "QR_TOKEN_EXPIRED", Message = "QR expired" };
        }

        if (qr.UseCount >= qr.MaxUses)
        {
            if (qr.Type == QrType.Payment || qr.Type == QrType.PaymentRequest)
            {
                return new QrResolveResult { Success = false, ErrorCode = "QR_PAYMENT_ALREADY_USED", Message = "Payment QR already used" };
            }

            return new QrResolveResult { Success = false, ErrorCode = "QR_TOKEN_EXPIRED", Message = "QR usage limit reached" };
        }

        qr.UseCount += 1;
        repository.SaveQrToken(qr);
        repository.AddAudit(new AuditEvent { EventType = "QR_SCANNED", SubjectId = qr.SubjectId, QrId = qr.Id, Details = "QR resolved" });

        var account = repository.GetOrCreateAccount(qr.SubjectId);
        var preview = privacyResolver.BuildPreview(account);

        return new QrResolveResult
        {
            Success = true,
            Type = qr.Type,
            Purpose = qr.Purpose,
            Recipient = preview,
            Message = "QR resolved"
        };
    }

    private string BuildSignedToken(QrToken qr)
    {
        var payload = new QrPayload
        {
            Id = qr.Id,
            Type = qr.Type,
            ExpiresAt = qr.ExpiresAt,
            IssuedAt = qr.CreatedAt
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signature = ComputeSignature(encodedPayload);
        return $"AQR_{encodedPayload}.{signature}";
    }

    private bool TryParseToken(string token, out QrPayload? payload, out string errorCode)
    {
        payload = null;
        errorCode = string.Empty;

        if (string.IsNullOrWhiteSpace(token) || !token.StartsWith("AQR_", StringComparison.Ordinal))
        {
            errorCode = "QR_SIGNATURE_INVALID";
            return false;
        }

        var body = token[4..];
        var parts = body.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            errorCode = "QR_SIGNATURE_INVALID";
            return false;
        }

        var payloadPart = parts[0];
        var signaturePart = parts[1];
        var expected = ComputeSignature(payloadPart);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(signaturePart), Encoding.UTF8.GetBytes(expected)))
        {
            errorCode = "QR_SIGNATURE_INVALID";
            return false;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(payloadPart));
            payload = JsonSerializer.Deserialize<QrPayload>(json);
            if (payload is null)
            {
                errorCode = "QR_SIGNATURE_INVALID";
                return false;
            }
        }
        catch
        {
            errorCode = "QR_SIGNATURE_INVALID";
            return false;
        }

        return true;
    }

    private string ComputeSignature(string payloadPart)
    {
        using var hmac = new HMACSHA256(_signingKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadPart));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var incoming = value.Replace('-', '+').Replace('_', '/');
        switch (incoming.Length % 4)
        {
            case 2:
                incoming += "==";
                break;
            case 3:
                incoming += "=";
                break;
        }

        return Convert.FromBase64String(incoming);
    }

    private sealed class QrPayload
    {
        public Guid Id { get; init; }
        public QrType Type { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
        public DateTimeOffset IssuedAt { get; init; }
    }
}

using Notification.Domain;

namespace Notification.Application;

public sealed class TemplateEngine
{
    public (string Subject, string Body, string Locale) Render(NotificationTemplate template, string preferredLocale, IReadOnlyDictionary<string, string> parameters)
    {
        if (!template.Localizations.TryGetValue(preferredLocale, out var variant))
        {
            variant = template.Localizations.TryGetValue("en", out var fallback)
                ? fallback
                : template.Localizations.Values.First();
            preferredLocale = template.Localizations.ContainsKey("en") ? "en" : template.Localizations.Keys.First();
        }

        return (ReplaceTokens(variant.Subject, parameters), ReplaceTokens(variant.Body, parameters), preferredLocale);
    }

    private static string ReplaceTokens(string template, IReadOnlyDictionary<string, string> parameters)
    {
        var content = template;
        foreach (var parameter in parameters)
        {
            content = content.Replace("{{" + parameter.Key + "}}", parameter.Value, StringComparison.OrdinalIgnoreCase);
        }

        return content;
    }
}

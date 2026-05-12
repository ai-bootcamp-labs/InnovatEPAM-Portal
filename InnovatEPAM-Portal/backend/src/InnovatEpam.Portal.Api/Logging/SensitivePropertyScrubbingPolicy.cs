using System.Globalization;
using Serilog.Core;
using Serilog.Events;

namespace InnovatEpam.Portal.Api.Logging;

/// <summary>
/// T113 / R7 — destructuring policy that masks sensitive properties so they
/// can never reach the log sink, regardless of which type happens to expose
/// them. Property names are matched case-insensitively against an allow-list
/// (<see cref="SensitivePropertyNames"/>) sourced from the constitution and
/// the auth/decision logging contract (T113a/T113b).
/// </summary>
public sealed class SensitivePropertyScrubbingPolicy : IDestructuringPolicy
{
    /// <summary>Property names whose values are replaced with <c>"***"</c>.</summary>
    public static readonly IReadOnlySet<string> SensitivePropertyNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password",
            "newPassword",
            "currentPassword",
            "passwordHash",
            "passwordConfirmation",
            "accessToken",
            "refreshToken",
            "authorization",
            "apiKey",
            "secret",
            "signingKey",
        };

    private const string Mask = "***";

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        ArgumentNullException.ThrowIfNull(propertyValueFactory);
        result = null!;
        if (value is null)
        {
            return false;
        }

        var type = value.GetType();
        if (type.IsPrimitive || value is string || value is DateTime || value is DateTimeOffset || value is Guid || value is decimal)
        {
            return false;
        }

        var properties = type.GetProperties()
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray();
        if (properties.Length == 0)
        {
            return false;
        }

        var typeTag = type.Name;
        var scrubbed = new List<LogEventProperty>(properties.Length);
        foreach (var property in properties)
        {
            object? propertyValue;
            try { propertyValue = property.GetValue(value); }
            catch { continue; }

            LogEventPropertyValue logValue = SensitivePropertyNames.Contains(property.Name)
                ? new ScalarValue(Mask)
                : propertyValueFactory.CreatePropertyValue(propertyValue, destructureObjects: true);

            scrubbed.Add(new LogEventProperty(property.Name, logValue));
        }

        result = new StructureValue(scrubbed, typeTag);
        return true;
    }

    /// <summary>Returns <c>true</c> when a property name is on the scrub list.</summary>
    public static bool IsSensitive(string propertyName) =>
        !string.IsNullOrEmpty(propertyName) && SensitivePropertyNames.Contains(propertyName);

    internal static string MaskValue => Mask;

    // Reserved for future culture-sensitive masking (e.g. partial credit-card masking).
    internal static string Format(IFormattable value, IFormatProvider? provider = null) =>
        value.ToString(format: null, provider ?? CultureInfo.InvariantCulture);
}

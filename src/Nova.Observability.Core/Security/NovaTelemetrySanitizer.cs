using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Nova.Observability.Abstractions;

namespace Nova.Observability.Core;

public sealed class NovaTelemetrySanitizer
{
    private static readonly Regex CredentialPattern =
        new(
            @"\b(password|passwd|pwd|client[_-]?secret|api[_-]?key|access[_-]?token|refresh[_-]?token)\b(\s*[:=]\s*)([^,\s;]+)",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);

    private static readonly Regex AuthorizationPattern =
        new(
            @"\b(authorization)(\s*[:=]\s*)(bearer\s+)?([A-Za-z0-9\-._~+/]+=*)",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);

    private readonly bool _enabled;
    private readonly string _redactedValue;

    private readonly int _maxAttributeValueLength;
    private readonly int _maxLogMessageLength;
    private readonly int _maxExceptionMessageLength;
    private readonly int _maxExceptionStackTraceLength;

    private readonly bool _replaceBinaryValues;
    private readonly bool _replaceComplexValues;

    private readonly string[] _sensitiveKeyFragments;

    private readonly Regex[] _additionalPatterns;

    public NovaTelemetrySanitizer(
        NovaDataProtectionOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        _enabled =
            options.Enabled;

        _redactedValue =
            string.IsNullOrWhiteSpace(options.RedactedValue)
                ? "[REDACTED]"
                : options.RedactedValue;

        _maxAttributeValueLength =
            NormalizeLength(
                options.MaxAttributeValueLength,
                2048);

        _maxLogMessageLength =
            NormalizeLength(
                options.MaxLogMessageLength,
                8192);

        _maxExceptionMessageLength =
            NormalizeLength(
                options.MaxExceptionMessageLength,
                4096);

        _maxExceptionStackTraceLength =
            NormalizeLength(
                options.MaxExceptionStackTraceLength,
                16384);

        _replaceBinaryValues =
            options.ReplaceBinaryValues;

        _replaceComplexValues =
            options.ReplaceComplexValues;

        _sensitiveKeyFragments =
            CopySensitiveKeys(
                options.SensitiveKeyFragments);

        _additionalPatterns =
            CompileAdditionalPatterns(
                options.AdditionalRedactionPatterns);
    }

    public object? ProtectAttribute(
        string key,
        object? value)
    {
        if (!_enabled ||
            value == null)
        {
            return value;
        }

        if (IsSensitiveKey(key))
            return _redactedValue;

        if (value is string text)
        {
            return ProtectText(
                text,
                _maxAttributeValueLength);
        }

        if (value is byte[] bytes)
        {
            if (!_replaceBinaryValues)
                return bytes;

            return "<binary:" +
                   bytes.Length.ToString(
                       CultureInfo.InvariantCulture) +
                   " bytes>";
        }

        if (IsPrimitiveTelemetryValue(value))
            return value;

        if (_replaceComplexValues)
        {
            return "<complex:" +
                   value.GetType().Name +
                   ">";
        }

        return ProtectText(
            value.ToString(),
            _maxAttributeValueLength);
    }

    public string? ProtectLogMessage(
        string? value)
    {
        return ProtectText(
            value,
            _maxLogMessageLength);
    }

    public string? ProtectExceptionMessage(
        string? value)
    {
        return ProtectText(
            value,
            _maxExceptionMessageLength);
    }

    public string? ProtectExceptionStackTrace(
        Exception? exception)
    {
        if (exception == null)
            return null;

        return ProtectText(
            exception.ToString(),
            _maxExceptionStackTraceLength);
    }

    private string? ProtectText(
        string? value,
        int maxLength)
    {
        if (!_enabled ||
            string.IsNullOrEmpty(value))
        {
            return value;
        }

        var protectedValue =
            CredentialPattern.Replace(
                value,
                match =>
                    match.Groups[1].Value +
                    match.Groups[2].Value +
                    _redactedValue);

        protectedValue =
            AuthorizationPattern.Replace(
                protectedValue,
                match =>
                    match.Groups[1].Value +
                    match.Groups[2].Value +
                    (match.Groups[3].Success
                        ? match.Groups[3].Value
                        : string.Empty) +
                    _redactedValue);

        for (var index = 0;
             index < _additionalPatterns.Length;
             index++)
        {
            protectedValue =
                _additionalPatterns[index]
                    .Replace(
                        protectedValue,
                        _redactedValue);
        }

        return Truncate(
            protectedValue,
            maxLength);
    }

    private bool IsSensitiveKey(
        string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        var normalized =
            NormalizeKey(key);

        for (var index = 0;
             index < _sensitiveKeyFragments.Length;
             index++)
        {
            if (normalized.IndexOf(
                    _sensitiveKeyFragments[index],
                    StringComparison.OrdinalIgnoreCase)
                >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeKey(
        string value)
    {
        var builder =
            new StringBuilder(
                value.Length);

        for (var index = 0;
             index < value.Length;
             index++)
        {
            var character =
                value[index];

            if (char.IsLetterOrDigit(
                    character))
            {
                builder.Append(
                    char.ToLowerInvariant(
                        character));
            }
        }

        return builder.ToString();
    }

    private static string[] CopySensitiveKeys(
        IEnumerable<string> values)
    {
        var result =
            new List<string>();

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            result.Add(
                NormalizeKey(value));
        }

        return result.ToArray();
    }

    private static Regex[] CompileAdditionalPatterns(
        IEnumerable<string> patterns)
    {
        var result =
            new List<Regex>();

        foreach (var pattern in patterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            try
            {
                result.Add(
                    new Regex(
                        pattern,
                        RegexOptions.IgnoreCase |
                        RegexOptions.CultureInvariant));
            }
            catch
            {
                /*
                 * Bir redaction regex hatası
                 * business uygulamasını durduramaz.
                 */
            }
        }

        return result.ToArray();
    }

    private static bool IsPrimitiveTelemetryValue(
        object value)
    {
        return value is bool ||
               value is byte ||
               value is sbyte ||
               value is short ||
               value is ushort ||
               value is int ||
               value is uint ||
               value is long ||
               value is ulong ||
               value is float ||
               value is double ||
               value is decimal;
    }

    private static string? Truncate(
        string? value,
        int maxLength)
    {
        if (string.IsNullOrEmpty(value) ||
            value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(
                   0,
                   maxLength) +
               "...[TRUNCATED]";
    }

    private static int NormalizeLength(
        int value,
        int defaultValue)
    {
        return value > 0
            ? value
            : defaultValue;
    }
}
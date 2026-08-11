using System;
using System.Collections.Generic;
using Nova.Observability.Abstractions;
using Nova.Observability.Core;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Nova.Observability.OpenTelemetry;

internal sealed class NovaLogDataProtectionProcessor :
    BaseProcessor<LogRecord>
{
    private readonly NovaTelemetrySanitizer
        _sanitizer;

    internal NovaLogDataProtectionProcessor(
        NovaDataProtectionOptions options)
    {
        _sanitizer =
            new NovaTelemetrySanitizer(
                options);
    }

    public override void OnEnd(
        LogRecord logRecord)
    {
        try
        {
            ProtectAttributes(
                logRecord);

            logRecord.FormattedMessage =
                _sanitizer
                    .ProtectLogMessage(
                        logRecord.FormattedMessage);

            logRecord.Body =
                _sanitizer
                    .ProtectLogMessage(
                        logRecord.Body);

            ProtectException(
                logRecord);
        }
        catch
        {
            /*
             * Redaction processor hiçbir koşulda
             * application code'a exception taşıyamaz.
             */
        }
    }

    private void ProtectAttributes(
        LogRecord logRecord)
    {
        var current =
            logRecord.Attributes;

        if (current == null)
            return;

        var protectedAttributes =
            new List<
                KeyValuePair<string, object?>>(
                    current.Count);

        for (var index = 0;
             index < current.Count;
             index++)
        {
            var item =
                current[index];

            protectedAttributes.Add(
                new KeyValuePair<
                    string,
                    object?>(
                    item.Key,
                    _sanitizer
                        .ProtectAttribute(
                            item.Key,
                            item.Value)));
        }

        logRecord.Attributes =
            protectedAttributes;
    }

    private void ProtectException(
        LogRecord logRecord)
    {
        var exception =
            logRecord.Exception;

        if (exception == null)
            return;

        var attributes =
            logRecord.Attributes != null
                ? new List<KeyValuePair<string, object?>>(
                    logRecord.Attributes)
                : new List<KeyValuePair<string, object?>>();

        RemoveAttribute(
            attributes,
            TelemetryTags.ExceptionType);

        RemoveAttribute(
            attributes,
            TelemetryTags.ExceptionMessage);

        RemoveAttribute(
            attributes,
            TelemetryTags.ExceptionStackTrace);

        attributes.Add(
            new KeyValuePair<string, object?>(
                TelemetryTags.ExceptionType,
                exception.GetType().FullName
                ?? exception.GetType().Name));

        attributes.Add(
            new KeyValuePair<string, object?>(
                TelemetryTags.ExceptionMessage,
                _sanitizer
                    .ProtectExceptionMessage(
                        exception.Message)));

        attributes.Add(
            new KeyValuePair<string, object?>(
                TelemetryTags.ExceptionStackTrace,
                _sanitizer
                    .ProtectExceptionStackTrace(
                        exception)));

        logRecord.Attributes =
            attributes;

        /*
         * Ham Exception exporter tarafından tekrar
         * serialize edilmesin.
         *
         * Güvenli type/message/stacktrace zaten
         * attribute olarak eklendi.
         */
        logRecord.Exception =
            null;
    }

    private static void RemoveAttribute(
        IList<KeyValuePair<string, object?>> values,
        string key)
    {
        for (var index = values.Count - 1;
             index >= 0;
             index--)
        {
            if (string.Equals(
                    values[index].Key,
                    key,
                    StringComparison.Ordinal))
            {
                values.RemoveAt(
                    index);
            }
        }
    }
}
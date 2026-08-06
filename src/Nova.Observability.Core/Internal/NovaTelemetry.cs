using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Nova.Observability.Abstractions;

namespace Nova.Observability.Core;

public static class NovaTelemetry
{
    internal static readonly ActivitySource ActivitySource =
        new(
            TelemetryNames.InstrumentationName,
            TelemetryNames.InstrumentationVersion);

    internal static readonly Meter Meter =
        new(
            TelemetryNames.InstrumentationName,
            TelemetryNames.InstrumentationVersion);

    internal static readonly Counter<long> OperationStarted =
        Meter.CreateCounter<long>(
            TelemetryNames.OperationStartedMetricName,
            unit: "{operation}",
            description: "Başlatılan Nova operasyonlarının sayısı.");

    internal static readonly Counter<long> OperationExecutions =
        Meter.CreateCounter<long>(
            TelemetryNames.OperationExecutionsMetricName,
            unit: "{operation}",
            description: "Sonuçlanan Nova operasyonlarının sayısı.");

    internal static readonly UpDownCounter<long> ActiveOperations =
        Meter.CreateUpDownCounter<long>(
            TelemetryNames.OperationActiveMetricName,
            unit: "{operation}",
            description: "Aktif olarak çalışan Nova operasyonlarının sayısı.");

    internal static readonly Histogram<double> OperationDuration =
        Meter.CreateHistogram<double>(
            TelemetryNames.OperationDurationMetricName,
            unit: "s",
            description: "Nova operasyonlarının saniye cinsinden süresi.");

    public static string? CurrentTraceId =>
        System.Diagnostics.Activity.Current?.TraceId.ToString();

    public static string? CurrentSpanId =>
        System.Diagnostics.Activity.Current?.SpanId.ToString();

    public static INovaOperation StartOperation(
        string operationName,
        NovaOperationOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(operationName))
        {
            throw new ArgumentException(
                "Operation name cannot be empty.",
                nameof(operationName));
        }

        return new NovaOperation(
            operationName,
            options ?? new NovaOperationOptions());
    }

    public static void AddEvent(
        string eventName,
        string? displayMessage = null,
        IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new ArgumentException(
                "Event name cannot be empty.",
                nameof(eventName));
        }

        var activity = System.Diagnostics.Activity.Current;

        if (activity == null)
            return;

        var eventTags =
            TelemetryTagHelper.CreateActivityTags(tags);

        eventTags[TelemetryTags.EventName] = eventName;

        if (!string.IsNullOrWhiteSpace(displayMessage))
        {
            eventTags[TelemetryTags.DisplayMessage] =
                displayMessage;
        }

        activity.AddEvent(
            new ActivityEvent(
                eventName,
                default(DateTimeOffset),
                eventTags));
    }
}
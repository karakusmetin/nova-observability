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

    private static readonly long RuntimeStartedTimestamp =
        Stopwatch.GetTimestamp();

    internal static readonly Counter<long> OperationStarted =
        Meter.CreateCounter<long>(
            TelemetryNames.OperationStartedMetricName,
            unit: "{operation}",
            description:
                "Başlatılan Nova operasyonlarının sayısı.");

    internal static readonly Counter<long> OperationExecutions =
        Meter.CreateCounter<long>(
            TelemetryNames.OperationExecutionsMetricName,
            unit: "{operation}",
            description:
                "Sonuçlanan Nova operasyonlarının sayısı.");

    internal static readonly UpDownCounter<long> ActiveOperations =
        Meter.CreateUpDownCounter<long>(
            TelemetryNames.OperationActiveMetricName,
            unit: "{operation}",
            description:
                "Aktif olarak çalışan Nova operasyonlarının sayısı.");

    internal static readonly Histogram<double> OperationDuration =
        Meter.CreateHistogram<double>(
            TelemetryNames.OperationDurationMetricName,
            unit: "s",
            description:
                "Nova operasyonlarının saniye cinsinden süresi.");

    internal static readonly ObservableGauge<int> ServiceAlive =
        Meter.CreateObservableGauge<int>(
            TelemetryNames.ServiceAliveMetricName,
            ObserveServiceAlive,
            unit: "{service}",
            description:
                "Nova telemetry pipeline tarafından gözlemlenen servis canlılık değeri.");

    internal static readonly ObservableGauge<double> ServiceUptime =
        Meter.CreateObservableGauge<double>(
            TelemetryNames.ServiceUptimeMetricName,
            ObserveServiceUptime,
            unit: "s",
            description:
                "Nova runtime başlangıcından itibaren çalışma süresi.");

    internal static readonly ObservableGauge<long>
        ServiceHeartbeatTimestamp =
            Meter.CreateObservableGauge<long>(
                TelemetryNames
                    .ServiceHeartbeatTimestampMetricName,
                ObserveHeartbeatTimestamp,
                unit: "s",
                description:
                    "Son metric collection anının Unix timestamp değeri.");

    public static string? CurrentTraceId =>
        Activity.Current?.TraceId.ToString();

    public static string? CurrentSpanId =>
        Activity.Current?.SpanId.ToString();

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

        var activity =
            Activity.Current;

        if (activity == null)
            return;

        var eventTags =
            TelemetryTagHelper.CreateActivityTags(
                tags);

        eventTags[TelemetryTags.EventName] =
            eventName;

        if (!string.IsNullOrWhiteSpace(
                displayMessage))
        {
            eventTags[
                TelemetryTags.DisplayMessage] =
                    displayMessage;
        }

        activity.AddEvent(
            new ActivityEvent(
                eventName,
                default(DateTimeOffset),
                eventTags));
    }

    private static int ObserveServiceAlive()
    {
        return 1;
    }

    private static double ObserveServiceUptime()
    {
        try
        {
            var elapsedTicks =
                Stopwatch.GetTimestamp() -
                RuntimeStartedTimestamp;

            return elapsedTicks /
                   (double)Stopwatch.Frequency;
        }
        catch
        {
            return 0;
        }
    }

    private static long ObserveHeartbeatTimestamp()
    {
        try
        {
            return DateTimeOffset
                .UtcNow
                .ToUnixTimeSeconds();
        }
        catch
        {
            return 0;
        }
    }
    public static void AddStep(
    string stepName,
    string? displayMessage = null,
    IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(
                stepName))
        {
            throw new ArgumentException(
                "Step name cannot be empty.",
                nameof(stepName));
        }

        var activity = Activity.Current;

        if (activity == null)
            return;

        var eventTags =
            TelemetryTagHelper
                .CreateActivityTags(
                    tags);

        eventTags[
            TelemetryTags.EventName] =
                TelemetryNames
                    .OperationStepEventName;

        eventTags[
            TelemetryTags.OperationStepName] =
                stepName;

        if (!string.IsNullOrWhiteSpace(
                displayMessage))
        {
            eventTags[
                TelemetryTags.DisplayMessage] =
                    displayMessage;
        }

        activity.AddEvent(
            new ActivityEvent(
                TelemetryNames.OperationStepEventName,
                default(DateTimeOffset),
                eventTags));
    }
}
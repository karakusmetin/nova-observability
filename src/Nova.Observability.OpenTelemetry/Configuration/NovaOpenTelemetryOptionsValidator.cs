using System;
using System.Collections.Generic;

namespace Nova.Observability.OpenTelemetry;

public static class NovaOpenTelemetryOptionsValidator
{
    public static bool TryValidate(
        NovaOpenTelemetryOptions options,
        out string? errorMessage)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var errors =
            new List<string>();

        if (string.IsNullOrWhiteSpace(
                options.ServiceName))
        {
            errors.Add(
                "ServiceName boş olamaz.");
        }

        if (!options.EnableConsoleExporter &&
            !options.EnableOtlpExporter)
        {
            errors.Add(
                "En az bir exporter etkin olmalıdır.");
        }

        if (options.EnableOtlpExporter)
        {
            if (options.OtlpEndpoint == null)
            {
                errors.Add(
                    "OTLP endpoint belirtilmelidir.");
            }
            else if (!options.OtlpEndpoint.IsAbsoluteUri)
            {
                errors.Add(
                    "OTLP endpoint absolute URI olmalıdır.");
            }
        }

        if (options.TraceSamplingRatio < 0 ||
            options.TraceSamplingRatio > 1)
        {
            errors.Add(
                "TraceSamplingRatio 0 ile 1 arasında olmalıdır.");
        }

        ValidateBatch(
            errors,
            "Trace",
            options.TraceMaxQueueSize,
            options.TraceMaxExportBatchSize);

        ValidateBatch(
            errors,
            "Log",
            options.LogMaxQueueSize,
            options.LogMaxExportBatchSize);

        if (options.ExporterTimeoutMilliseconds <= 0)
        {
            errors.Add(
                "ExporterTimeoutMilliseconds sıfırdan büyük olmalıdır.");
        }

        if (options.TraceScheduledDelayMilliseconds <= 0)
        {
            errors.Add(
                "TraceScheduledDelayMilliseconds sıfırdan büyük olmalıdır.");
        }

        if (options.LogScheduledDelayMilliseconds <= 0)
        {
            errors.Add(
                "LogScheduledDelayMilliseconds sıfırdan büyük olmalıdır.");
        }

        if (options.MetricExportIntervalMilliseconds <= 0)
        {
            errors.Add(
                "MetricExportIntervalMilliseconds sıfırdan büyük olmalıdır.");
        }

        if (options.MetricExportTimeoutMilliseconds <= 0)
        {
            errors.Add(
                "MetricExportTimeoutMilliseconds sıfırdan büyük olmalıdır.");
        }

        if (options.ShutdownFlushTimeoutMilliseconds <= 0)
        {
            errors.Add(
                "ShutdownFlushTimeoutMilliseconds sıfırdan büyük olmalıdır.");
        }

        if (errors.Count == 0)
        {
            errorMessage = null;
            return true;
        }

        errorMessage =
            string.Join(" ", errors);

        return false;
    }

    private static void ValidateBatch(
        ICollection<string> errors,
        string name,
        int queueSize,
        int batchSize)
    {
        if (queueSize <= 0)
        {
            errors.Add(
                name +
                " queue size sıfırdan büyük olmalıdır.");
        }

        if (batchSize <= 0)
        {
            errors.Add(
                name +
                " batch size sıfırdan büyük olmalıdır.");
        }

        if (batchSize > queueSize)
        {
            errors.Add(
                name +
                " batch size queue size değerinden büyük olamaz.");
        }
    }
}
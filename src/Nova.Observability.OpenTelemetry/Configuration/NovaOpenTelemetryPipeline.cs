using Nova.Observability.Abstractions;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;

namespace Nova.Observability.OpenTelemetry;

public static class NovaOpenTelemetryPipeline
{
    private const string DeploymentEnvironmentName =
        "deployment.environment.name";

    public static TracerProviderBuilder ConfigureTracing(
        TracerProviderBuilder builder,
        NovaOpenTelemetryOptions options)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        if (options == null)
            throw new ArgumentNullException(nameof(options));

        builder
            .SetResourceBuilder(
                CreateResourceBuilder(options))
            .SetSampler(
                CreateSampler(options.TraceSamplingRatio))
            .AddSource(
                TelemetryNames.InstrumentationName);

        if (options.EnableConsoleExporter)
        {
            builder.AddConsoleExporter();
        }

        if (options.EnableOtlpExporter)
        {
            builder.AddOtlpExporter(exporterOptions =>
            {
                ConfigureCommonExporter(
                    exporterOptions,
                    options,
                    ResolveSignalEndpoint(
                        options,
                        "traces"));

                exporterOptions.ExportProcessorType =
                    ExportProcessorType.Batch;

                exporterOptions
                    .BatchExportProcessorOptions
                    .MaxQueueSize =
                        options.TraceMaxQueueSize;

                exporterOptions
                    .BatchExportProcessorOptions
                    .MaxExportBatchSize =
                        options.TraceMaxExportBatchSize;

                exporterOptions
                    .BatchExportProcessorOptions
                    .ScheduledDelayMilliseconds =
                        options.TraceScheduledDelayMilliseconds;

                exporterOptions
                    .BatchExportProcessorOptions
                    .ExporterTimeoutMilliseconds =
                        options.ExporterTimeoutMilliseconds;
            });
        }

        return builder;
    }

    public static MeterProviderBuilder ConfigureMetrics(
        MeterProviderBuilder builder,
        NovaOpenTelemetryOptions options)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        if (options == null)
            throw new ArgumentNullException(nameof(options));

        builder
            .SetResourceBuilder(
                CreateResourceBuilder(options))
            .AddMeter(
                TelemetryNames.InstrumentationName);

        if (options.EnableConsoleExporter)
        {
            builder.AddConsoleExporter();
        }

        if (options.EnableOtlpExporter)
        {
            builder.AddOtlpExporter(
                (exporterOptions, readerOptions) =>
                {
                    ConfigureCommonExporter(
                        exporterOptions,
                        options,
                        ResolveSignalEndpoint(
                            options,
                            "metrics"));

                    readerOptions
                        .PeriodicExportingMetricReaderOptions
                        .ExportIntervalMilliseconds =
                            options
                                .MetricExportIntervalMilliseconds;

                    readerOptions
                        .PeriodicExportingMetricReaderOptions
                        .ExportTimeoutMilliseconds =
                            options
                                .MetricExportTimeoutMilliseconds;
                });
        }

        return builder;
    }

    public static OpenTelemetryLoggerOptions ConfigureLogging(
        OpenTelemetryLoggerOptions logging,
        NovaOpenTelemetryOptions options)
    {
        if (logging == null)
            throw new ArgumentNullException(nameof(logging));

        if (options == null)
            throw new ArgumentNullException(nameof(options));

        logging.IncludeFormattedMessage =
            options.IncludeFormattedLogMessage;

        logging.IncludeScopes =
            options.IncludeLogScopes;

        logging.ParseStateValues =
            options.ParseLogStateValues;

        logging.SetResourceBuilder(
            CreateResourceBuilder(options));

        if (options.EnableConsoleExporter)
        {
            logging.AddConsoleExporter();
        }

        if (options.EnableOtlpExporter)
        {
            logging.AddOtlpExporter(
                (exporterOptions, processorOptions) =>
                {
                    ConfigureCommonExporter(
                        exporterOptions,
                        options,
                        ResolveSignalEndpoint(
                            options,
                            "logs"));

                    processorOptions
                        .BatchExportProcessorOptions
                        .MaxQueueSize =
                            options.LogMaxQueueSize;

                    processorOptions
                        .BatchExportProcessorOptions
                        .MaxExportBatchSize =
                            options.LogMaxExportBatchSize;

                    processorOptions
                        .BatchExportProcessorOptions
                        .ScheduledDelayMilliseconds =
                            options
                                .LogScheduledDelayMilliseconds;

                    processorOptions
                        .BatchExportProcessorOptions
                        .ExporterTimeoutMilliseconds =
                            options
                                .ExporterTimeoutMilliseconds;
                });
        }

        return logging;
    }

    public static ResourceBuilder CreateResourceBuilder(
        NovaOpenTelemetryOptions options)
    {
        var builder =
            ResourceBuilder
                .CreateDefault()
                .AddService(
                    serviceName:
                        options.ServiceName,

                    serviceNamespace:
                        options.ServiceNamespace,

                    serviceVersion:
                        options.ServiceVersion,

                    serviceInstanceId:
                        options.ServiceInstanceId);

        if (!string.IsNullOrWhiteSpace(
                options.EnvironmentName))
        {
            builder.AddAttributes(
                new[]
                {
                    new KeyValuePair<string, object>(
                        DeploymentEnvironmentName,
                        options.EnvironmentName!)
                });
        }

        if (options.ResourceAttributes.Count > 0)
        {
            builder.AddAttributes(
                options.ResourceAttributes);
        }

        return builder;
    }

    private static Sampler CreateSampler(
        double samplingRatio)
    {
        if (samplingRatio <= 0)
        {
            return new ParentBasedSampler(
                new AlwaysOffSampler());
        }

        if (samplingRatio >= 1)
        {
            return new ParentBasedSampler(
                new AlwaysOnSampler());
        }

        return new ParentBasedSampler(
            new TraceIdRatioBasedSampler(
                samplingRatio));
    }

    private static void ConfigureCommonExporter(
        OtlpExporterOptions exporterOptions,
        NovaOpenTelemetryOptions options,
        Uri endpoint)
    {
        exporterOptions.Endpoint =
            endpoint;

        exporterOptions.Protocol =
            options.OtlpProtocol ==
            NovaOtlpProtocol.Grpc
                ? OtlpExportProtocol.Grpc
                : OtlpExportProtocol.HttpProtobuf;

        exporterOptions.TimeoutMilliseconds =
            options.ExporterTimeoutMilliseconds;

        if (!string.IsNullOrWhiteSpace(
                options.OtlpHeaders))
        {
            exporterOptions.Headers =
                options.OtlpHeaders;
        }
    }

    private static Uri ResolveSignalEndpoint(
        NovaOpenTelemetryOptions options,
        string signalName)
    {
        if (options.OtlpProtocol ==
            NovaOtlpProtocol.Grpc)
        {
            return options.OtlpEndpoint;
        }

        var baseAddress =
            options
                .OtlpEndpoint
                .AbsoluteUri
                .TrimEnd('/') + "/";

        return new Uri(
            new Uri(baseAddress),
            "v1/" + signalName);
    }
}
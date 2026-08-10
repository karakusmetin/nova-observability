using System;
using System.Collections.Generic;
using Nova.Observability.Abstractions;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Nova.Observability.OpenTelemetry;

public static class NovaOpenTelemetry
{
    private const string DeploymentEnvironmentName =
        "deployment.environment.name";

    public static NovaOpenTelemetryRuntime Start(
        NovaOpenTelemetryOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (!options.Enabled)
        {
            return CreateDisabledRuntime(
                options,
                initializationException: null);
        }

        var validationException =
            Validate(options);

        if (validationException != null)
        {
            return HandleInitializationFailure(
                options,
                validationException);
        }
        
        if (!NovaOpenTelemetryOptionsValidator.TryValidate(
        options,
        out var validationError))
        {
            return HandleInitializationFailure(
                options,
                new InvalidOperationException(
                    validationError));
        }

        TracerProvider? tracerProvider = null;
        MeterProvider? meterProvider = null;

        try
        {
            var resourceBuilder =
                CreateResourceBuilder(options);

            tracerProvider =
                NovaOpenTelemetryPipeline
                    .ConfigureTracing(
                        Sdk.CreateTracerProviderBuilder(),
                        options)
                    .Build();

            meterProvider =
                NovaOpenTelemetryPipeline
                    .ConfigureMetrics(
                        Sdk.CreateMeterProviderBuilder(),
                        options)
                    .Build();

            NovaOpenTelemetryDiagnostics.Report(
                options,
                "Nova OpenTelemetry başarıyla başlatıldı.");

            return new NovaOpenTelemetryRuntime(
                options,
                tracerProvider,
                meterProvider,
                initializationException: null);
        }
        catch (Exception exception)
        {
            NovaOpenTelemetryDiagnostics.DisposeSafely(
                meterProvider,
                options,
                nameof(MeterProvider));

            NovaOpenTelemetryDiagnostics.DisposeSafely(
                tracerProvider,
                options,
                nameof(TracerProvider));

            return HandleInitializationFailure(
                options,
                exception);
        }
    }
    private static ResourceBuilder CreateResourceBuilder(
        NovaOpenTelemetryOptions options)
    {
        var builder =
            ResourceBuilder
                .CreateDefault()
                .AddService(
                    serviceName: options.ServiceName,
                    serviceNamespace: options.ServiceNamespace,
                    serviceVersion: options.ServiceVersion,
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
        exporterOptions.Endpoint = endpoint;

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

    private static Exception? Validate(
        NovaOpenTelemetryOptions options)
    {
        var errors = new List<string>();

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

        if (options.EnableOtlpExporter &&
            options.OtlpEndpoint == null)
        {
            errors.Add(
                "OTLP endpoint belirtilmelidir.");
        }

        if (options.TraceSamplingRatio < 0 ||
            options.TraceSamplingRatio > 1)
        {
            errors.Add(
                "TraceSamplingRatio 0 ile 1 arasında olmalıdır.");
        }

        if (options.TraceMaxQueueSize <= 0)
        {
            errors.Add(
                "TraceMaxQueueSize sıfırdan büyük olmalıdır.");
        }

        if (options.TraceMaxExportBatchSize <= 0)
        {
            errors.Add(
                "TraceMaxExportBatchSize sıfırdan büyük olmalıdır.");
        }

        if (options.TraceMaxExportBatchSize >
            options.TraceMaxQueueSize)
        {
            errors.Add(
                "TraceMaxExportBatchSize, TraceMaxQueueSize değerinden büyük olamaz.");
        }

        if (options.ExporterTimeoutMilliseconds <= 0)
        {
            errors.Add(
                "ExporterTimeoutMilliseconds sıfırdan büyük olmalıdır.");
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

        if (errors.Count == 0)
            return null;

        return new InvalidOperationException(
            "Nova OpenTelemetry yapılandırması geçersiz: " +
            string.Join(" ", errors));
    }

    private static NovaOpenTelemetryRuntime
        HandleInitializationFailure(
            NovaOpenTelemetryOptions options,
            Exception exception)
    {
        NovaOpenTelemetryDiagnostics.Report(
            options,
            "Nova OpenTelemetry başlatılamadı. Uygulama telemetry olmadan çalışacak.",
            exception);

        if (options.InitializationFailureMode ==
            NovaTelemetryInitializationFailureMode.Throw)
        {
            throw new InvalidOperationException(
                "Nova OpenTelemetry başlatılamadı.",
                exception);
        }

        return CreateDisabledRuntime(
            options,
            exception);
    }

    private static NovaOpenTelemetryRuntime
        CreateDisabledRuntime(
            NovaOpenTelemetryOptions options,
            Exception? initializationException)
    {
        return new NovaOpenTelemetryRuntime(
            options,
            tracerProvider: null,
            meterProvider: null,
            initializationException);
    }
}
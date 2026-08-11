using Nova.Observability.Core;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System;

namespace Nova.Observability.OpenTelemetry;

public static class NovaOpenTelemetry
{

    public static NovaOpenTelemetryRuntime Start(
        NovaOpenTelemetryOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(
                nameof(options));
        
        if (!options.Enabled)
        {
            return CreateDisabledRuntime(
                options,
                initializationException: null);
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

        NovaTelemetry.ConfigureDataProtection(options.DataProtection);

        TracerProvider? tracerProvider =
            null;

        MeterProvider? meterProvider =
            null;

        try
        {
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
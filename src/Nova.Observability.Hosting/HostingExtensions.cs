using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nova.Observability.OpenTelemetry;
using OpenTelemetry;

namespace Nova.Observability.Hosting;

public static class HostingExtensions
{
    public static IServiceCollection AddNovaObservability(
        this IServiceCollection services,
        Action<NovaOpenTelemetryOptions> configure)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var options =
            new NovaOpenTelemetryOptions();

        configure(options);

        if (!options.Enabled)
        {
            services.TryAddSingleton(
                new NovaObservabilityState(
                    isEnabled: false,
                    disabledReason:
                        "Nova Observability configuration ile devre dışı bırakıldı."));

            return services;
        }

        if (!NovaOpenTelemetryOptionsValidator.TryValidate(
                options,
                out var validationError))
        {
            ReportDiagnosticSafely(
                options,
                "Nova Observability yapılandırması geçersiz. " +
                "Telemetry devre dışı bırakılacak.",
                new InvalidOperationException(
                    validationError));

            if (options.InitializationFailureMode ==
                NovaTelemetryInitializationFailureMode.Throw)
            {
                throw new InvalidOperationException(
                    "Nova Observability yapılandırması geçersiz. " +
                    validationError);
            }

            services.TryAddSingleton(
                new NovaObservabilityState(
                    isEnabled: false,
                    disabledReason:
                        validationError));

            return services;
        }

        services.TryAddSingleton(options);

        services.TryAddSingleton(
            new NovaObservabilityState(
                isEnabled: true,
                disabledReason: null));

        var openTelemetry =
            services.AddOpenTelemetry();

        openTelemetry.WithTracing(
            tracing =>
            {
                NovaOpenTelemetryPipeline.ConfigureTracing(
                    tracing,
                    options);
            });

        openTelemetry.WithMetrics(
            metrics =>
            {
                NovaOpenTelemetryPipeline.ConfigureMetrics(
                    metrics,
                    options);
            });

        openTelemetry.WithLogging(
            configureBuilder: null,
            configureOptions: logging =>
            {
                NovaOpenTelemetryPipeline.ConfigureLogging(
                    logging,
                    options);
            });

        return services;
    }

    private static void ReportDiagnosticSafely(
        NovaOpenTelemetryOptions options,
        string message,
        Exception? exception)
    {
        var handler =
            options.DiagnosticHandler;

        if (handler == null)
            return;

        try
        {
            handler(
                message,
                exception);
        }
        catch
        {
            // Telemetry diagnostic mekanizması
            // business uygulamasını etkileyemez.
        }
    }
}
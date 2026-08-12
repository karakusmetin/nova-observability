using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nova.Observability.OpenTelemetry;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Nova.Observability.Interception;
using Microsoft.Extensions.Configuration;

namespace Nova.Observability.Hosting;

public static class HostingExtensions
{
    private static IServiceCollection AddNovaObservability(
    IServiceCollection services,
    NovaOpenTelemetryOptions options)
    {
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

        services.TryAddSingleton(
            options);

        services.TryAddSingleton(
            new NovaInterceptionOptions
            {
                Enabled = true,

                EnableLogScopes = true,

                LogFailures = true,

                DiagnosticHandler =
                    options.DiagnosticHandler
            });

        services.TryAddSingleton<
            IProxyGenerator,
            ProxyGenerator>();

        services.TryAddSingleton<
            NovaOperationInterceptor>();

        services.TryAddSingleton(
            new NovaObservabilityState(
                isEnabled: true,
                disabledReason: null));

        var openTelemetry =
            services.AddOpenTelemetry();

        openTelemetry.WithTracing(
            tracing =>
            {
                NovaOpenTelemetryPipeline
                    .ConfigureTracing(
                        tracing,
                        options);
            });

        openTelemetry.WithMetrics(
            metrics =>
            {
                NovaOpenTelemetryPipeline
                    .ConfigureMetrics(
                        metrics,
                        options);
            });

        openTelemetry.WithLogging(
            configureBuilder: null,
            configureOptions:
                logging =>
                {
                    NovaOpenTelemetryPipeline
                        .ConfigureLogging(
                            logging,
                            options);
                });

        return services;
    }
    public static IServiceCollection AddNovaObservability(
    this IServiceCollection services,
    IConfiguration configuration,
    string sectionName = "Nova:Observability",
    Action<NovaOpenTelemetryOptions>? configure = null)
    {
        if (services == null)
            throw new ArgumentNullException(
                nameof(services));

        if (configuration == null)
            throw new ArgumentNullException(
                nameof(configuration));

        if (string.IsNullOrWhiteSpace(
                sectionName))
        {
            throw new ArgumentException(
                "Section name cannot be empty.",
                nameof(sectionName));
        }

        var options =
            new NovaOpenTelemetryOptions();

        configuration
            .GetSection(sectionName)
            .Bind(options);

        configure?.Invoke(
            options);

        return AddNovaObservability(
            services,
            options);
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
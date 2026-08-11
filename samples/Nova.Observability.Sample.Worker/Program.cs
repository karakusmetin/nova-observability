using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nova.Observability.Hosting;
using Nova.Observability.OpenTelemetry;
using Nova.Observability.Sample.Worker;
using Nova.Observability.Sample.Worker.Services;
using System;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddNovaObservability(
    options =>
    {
        options.ServiceName =
            "Nova.Observability.Sample.Worker";

        options.ServiceNamespace =
            "Nova";

        options.ServiceVersion =
            "0.1.0";

        options.ServiceInstanceId =
            Environment.MachineName;

        options.EnvironmentName =
            builder.Environment.EnvironmentName;

        /*
         * Bu committe önce Console üzerinden
         * ürettiğimiz telemetry'yi doğruluyoruz.
         *
         * Sonraki committe Aspire Dashboard için
         * OTLP'yi açacağız.
         */
        options.EnableConsoleExporter =
    false;

        options.EnableOtlpExporter =
            true;

        options.OtlpEndpoint =
            new Uri(
                "http://localhost:4317");

        options.OtlpProtocol =
            NovaOtlpProtocol.Grpc;

        /*
         * Demo sırasında bütün trace'leri görmek
         * istediğimiz için sampling %100.
         */
        options.TraceSamplingRatio =
            1.0;

        /*
         * Nova configuration hatası Worker'ı
         * durdurmasın.
         */
        options.InitializationFailureMode =
            NovaTelemetryInitializationFailureMode
                .ContinueWithoutTelemetry;

        options.DiagnosticHandler =
            (message, exception) =>
            {
                Console.Error.WriteLine(
                    "[Nova] " + message);

                if (exception != null)
                {
                    Console.Error.WriteLine(
                        exception);
                }
            };
    });

builder.Services
    .AddNovaObservedSingleton<
        ISampleMessageProcessor,
        SampleMessageProcessor>();

builder.Services.AddHostedService<
    Worker>();

var host =
    builder.Build();

await host.RunAsync();
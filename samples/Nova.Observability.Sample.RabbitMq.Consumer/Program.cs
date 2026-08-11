using Nova.Observability.Core;
using Nova.Observability.Hosting;
using Nova.Observability.OpenTelemetry;
using Nova.Observability.Sample.RabbitMq.Consumer;
using Nova.Observability.Sample.RabbitMq.Consumer.Services;

var builder =
    Host.CreateApplicationBuilder(args);

builder.Services.AddNovaObservability(
    options =>
    {
        options.ServiceName =
            "Nova.Sample.RabbitMq.Consumer";

        options.ServiceNamespace =
            "Nova";

        options.ServiceVersion =
            "0.1.0";

        options.EnvironmentName =
            builder.Environment.EnvironmentName;

        options.EnableConsoleExporter =
            false;

        options.EnableOtlpExporter =
            true;

        options.OtlpEndpoint =
            new Uri(
                "http://localhost:4317");

        options.OtlpProtocol =
            NovaOtlpProtocol.Grpc;

        NovaTelemetry.ConfigureDataProtection(options.DataProtection);
    });

builder.Services
    .AddNovaObservedSingleton<
        ISampleRabbitMessageHandler,
        SampleRabbitMessageHandler>();

builder.Services.AddHostedService<Worker>();

await builder
    .Build()
    .RunAsync();
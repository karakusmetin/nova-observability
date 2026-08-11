using Nova.Observability.Hosting;
using Nova.Observability.OpenTelemetry;
using Nova.Observability.Sample.RabbitMq.Publisher;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddNovaObservability(
    options =>
    {
        options.ServiceName =
            "Nova.Sample.RabbitMq.Publisher";

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
    });

builder.Services.AddHostedService<Worker>();

await builder
    .Build()
    .RunAsync();
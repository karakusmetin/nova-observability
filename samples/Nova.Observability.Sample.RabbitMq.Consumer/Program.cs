using Nova.Observability.Hosting;
using Nova.Observability.Sample.RabbitMq.Consumer;
using Nova.Observability.Sample.RabbitMq.Consumer.Services;

var builder =
    Host.CreateApplicationBuilder(args);

builder.Services.AddNovaObservability(
    builder.Configuration,
    "Nova:Observability",
    options =>
    {
        /*
         * Makineye özgü runtime değerini
         * config'e yazmak istemiyoruz.
         */
        options.ServiceInstanceId =
            Environment.MachineName;

        options.EnvironmentName =
            builder.Environment
                .EnvironmentName;

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
        ISampleRabbitMessageHandler,
        SampleRabbitMessageHandler>();

builder.Services.AddHostedService<Worker>();

await builder
    .Build()
    .RunAsync();
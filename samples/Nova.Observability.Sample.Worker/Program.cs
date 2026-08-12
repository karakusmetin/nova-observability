using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nova.Observability.File;
using Nova.Observability.Hosting;
using Nova.Observability.Sample.Worker;
using Nova.Observability.Sample.Worker.Services;
using System;

var builder = Host.CreateApplicationBuilder(args);

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
        ISampleMessageProcessor,
        SampleMessageProcessor>();

builder.Logging.AddNovaFile(
    builder.Configuration,
    "Nova:FileLogging",
    options =>
    {
        options.ServiceName =
            "Nova.Observability.Sample.Worker";

        options.EnvironmentName =
            builder.Environment.EnvironmentName;
    });


builder.Services.AddHostedService<
    Worker>();

var host =
    builder.Build();

await host.RunAsync();
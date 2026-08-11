using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Nova.Observability.Interception;
using Nova.Observability.OpenTelemetry;
using OpenTelemetry.Logs;

namespace Nova.Observability.Legacy
{
    public static class NovaLegacyTelemetry
    {
        private const string DefaultOtlpEndpoint =
            "http://localhost:4318/";

        public static NovaLegacyTelemetryRuntime
            StartFromAppConfig()
        {
            try
            {
                var options =
                    CreateOptionsFromAppConfig();

                return Start(options);
            }
            catch (Exception exception)
            {
                /*
                 * App.config hatası eski uygulamayı
                 * durdurmamalıdır.
                 */
                Trace.WriteLine(
                    "[Nova] App.config okunamadı. " +
                    exception);

                var disabledOptions =
                    new NovaOpenTelemetryOptions();

                disabledOptions.Enabled = false;
                disabledOptions.ServiceName =
                    ResolveDefaultServiceName();

                return Start(
                    disabledOptions);
            }
        }

        public static NovaLegacyTelemetryRuntime Start(
            NovaOpenTelemetryOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            /*
             * .NET Framework üzerinde OTLP/gRPC
             * desteklenmediğinden legacy adapter'ın
             * protokolü daima HTTP/protobuf.
             */
            options.OtlpProtocol =
                NovaOtlpProtocol.HttpProtobuf;

            var diagnosticHandler =
                CreateDiagnosticHandler(
                    options);

            var telemetryRuntime =
                NovaOpenTelemetry.Start(
                    options);

            ILoggerFactory loggerFactory;

            if (!telemetryRuntime.IsEnabled)
            {
                loggerFactory =
                    LoggerFactory.Create(
                        delegate (ILoggingBuilder builder)
                        {
                        });
            }
            else
            {
                try
                {
                    loggerFactory =
                        LoggerFactory.Create(
                            delegate (ILoggingBuilder builder)
                            {
                                builder.SetMinimumLevel(
                                    LogLevel.Information);

                                builder.AddOpenTelemetry(
                                    delegate (
                                        OpenTelemetryLoggerOptions logging)
                                    {
                                        NovaOpenTelemetryPipeline
                                            .ConfigureLogging(
                                                logging,
                                                options);
                                    });
                            });
                }
                catch (Exception exception)
                {
                    ReportSafely(
                        diagnosticHandler,
                        "Legacy logging pipeline başlatılamadı. " +
                        "Trace ve metric çalışmaya devam edecek.",
                        exception);

                    loggerFactory =
                        LoggerFactory.Create(
                            delegate (ILoggingBuilder builder)
                            {
                            });
                }
            }

            var interceptionOptions =
                new NovaInterceptionOptions();

            interceptionOptions.Enabled = true;
            interceptionOptions.EnableLogScopes = true;
            interceptionOptions.LogFailures = true;

            interceptionOptions.DiagnosticHandler =
                delegate (
                    string message,
                    Exception exception)
                {
                    ReportSafely(
                        diagnosticHandler,
                        message,
                        exception);
                };

            return new NovaLegacyTelemetryRuntime(
                telemetryRuntime,
                loggerFactory,
                interceptionOptions,
                diagnosticHandler);
        }

        private static NovaOpenTelemetryOptions
            CreateOptionsFromAppConfig()
        {
            var options =
                new NovaOpenTelemetryOptions();

            options.Enabled =
                ReadBool(
                    "Nova.Observability.Enabled",
                    true);

            options.ServiceName =
                ReadString(
                    "Nova.Observability.ServiceName",
                    ResolveDefaultServiceName());

            options.ServiceNamespace =
                ReadOptionalString(
                    "Nova.Observability.ServiceNamespace");

            options.ServiceVersion =
                ReadString(
                    "Nova.Observability.ServiceVersion",
                    ResolveDefaultServiceVersion());

            options.EnvironmentName =
                ReadString(
                    "Nova.Observability.Environment",
                    "Unknown");

            options.EnableOtlpExporter =
                ReadBool(
                    "Nova.Observability.EnableOtlpExporter",
                    true);

            options.EnableConsoleExporter =
                ReadBool(
                    "Nova.Observability.EnableConsoleExporter",
                    false);

            var endpoint =
                ReadString(
                    "Nova.Observability.OtlpEndpoint",
                    DefaultOtlpEndpoint);

            Uri endpointUri;

            if (!Uri.TryCreate(
                    endpoint,
                    UriKind.Absolute,
                    out endpointUri))
            {
                throw new ConfigurationErrorsException(
                    "Nova.Observability.OtlpEndpoint geçerli bir URI değil.");
            }

            options.OtlpEndpoint =
                endpointUri;

            options.OtlpProtocol =
                NovaOtlpProtocol.HttpProtobuf;

            options.OtlpHeaders =
                ReadOptionalString(
                    "Nova.Observability.OtlpHeaders");

            options.TraceSamplingRatio =
                ReadDouble(
                    "Nova.Observability.TraceSamplingRatio",
                    1.0);

            options.InitializationFailureMode =
                NovaTelemetryInitializationFailureMode
                    .ContinueWithoutTelemetry;

            options.DiagnosticHandler =
                delegate (
                    string message,
                    Exception exception)
                {
                    Trace.WriteLine(
                        "[Nova] " +
                        message +
                        (exception == null
                            ? string.Empty
                            : Environment.NewLine +
                              exception));
                };

            return options;
        }

        private static Action<string, Exception>
            CreateDiagnosticHandler(
                NovaOpenTelemetryOptions options)
        {
            return delegate (
                string message,
                Exception exception)
            {
                try
                {
                    if (options.DiagnosticHandler != null)
                    {
                        options.DiagnosticHandler(
                            message,
                            exception);
                    }
                }
                catch
                {
                }
            };
        }

        private static void ReportSafely(
            Action<string, Exception> handler,
            string message,
            Exception exception)
        {
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
            }
        }

        private static string ResolveDefaultServiceName()
        {
            var assembly =
                Assembly.GetEntryAssembly();

            if (assembly == null)
                return "legacy-service";

            return assembly
                       .GetName()
                       .Name
                   ?? "legacy-service";
        }

        private static string ResolveDefaultServiceVersion()
        {
            var assembly =
                Assembly.GetEntryAssembly();

            if (assembly == null)
                return "unknown";

            var version =
                assembly.GetName().Version;

            return version == null
                ? "unknown"
                : version.ToString();
        }

        private static string ReadString(
            string key,
            string defaultValue)
        {
            var value =
                ConfigurationManager
                    .AppSettings[key];

            return string.IsNullOrWhiteSpace(
                value)
                ? defaultValue
                : value;
        }

        private static string ReadOptionalString(
            string key)
        {
            var value =
                ConfigurationManager
                    .AppSettings[key];

            return string.IsNullOrWhiteSpace(
                value)
                ? null
                : value;
        }

        private static bool ReadBool(
            string key,
            bool defaultValue)
        {
            var value =
                ConfigurationManager
                    .AppSettings[key];

            bool parsed;

            return bool.TryParse(
                value,
                out parsed)
                ? parsed
                : defaultValue;
        }

        private static double ReadDouble(
            string key,
            double defaultValue)
        {
            var value =
                ConfigurationManager
                    .AppSettings[key];

            double parsed;

            return double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out parsed)
                ? parsed
                : defaultValue;
        }
    }
}
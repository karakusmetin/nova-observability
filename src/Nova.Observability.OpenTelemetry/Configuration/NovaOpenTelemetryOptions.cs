using System;
using System.Collections.Generic;

namespace Nova.Observability.OpenTelemetry;

public sealed class NovaOpenTelemetryOptions
{
    public bool Enabled { get; set; } = true;

    public string ServiceName { get; set; } =
        "unknown-service";

    public string? ServiceNamespace { get; set; }

    public string? ServiceVersion { get; set; }

    public string ServiceInstanceId { get; set; } =
        Environment.MachineName;

    public string? EnvironmentName { get; set; }

    public bool EnableOtlpExporter { get; set; } =
        true;

    /// <summary>
    /// Yalnızca geliştirme ve ilk doğrulama amacıyla kullanılmalıdır.
    /// </summary>
    public bool EnableConsoleExporter { get; set; }

    /// <summary>
    /// HTTP/protobuf için örnek:
    /// http://localhost:4318/
    ///
    /// gRPC için örnek:
    /// http://localhost:4317/
    /// </summary>
    public Uri OtlpEndpoint { get; set; } =
        new("http://localhost:4318/");

    public NovaOtlpProtocol OtlpProtocol { get; set; } =
        NovaOtlpProtocol.HttpProtobuf;

    public string? OtlpHeaders { get; set; }

    public int ExporterTimeoutMilliseconds { get; set; } =
        5_000;

    public int TraceMaxQueueSize { get; set; } =
        2_048;

    public int TraceMaxExportBatchSize { get; set; } =
        512;

    public int TraceScheduledDelayMilliseconds { get; set; } =
        5_000;

    public int MetricExportIntervalMilliseconds { get; set; } =
        10_000;

    public int MetricExportTimeoutMilliseconds { get; set; } =
        5_000;

    /// <summary>
    /// 0.0 hiçbir yeni trace'i örneklemez,
    /// 1.0 bütün trace'leri örnekler.
    /// </summary>
    public double TraceSamplingRatio { get; set; } =
        1.0;

    public int ShutdownFlushTimeoutMilliseconds { get; set; } =
        3_000;

    public NovaTelemetryInitializationFailureMode
        InitializationFailureMode
    { get; set; } =
            NovaTelemetryInitializationFailureMode
                .ContinueWithoutTelemetry;

    public IDictionary<string, object> ResourceAttributes { get; } =
        new Dictionary<string, object>(
            StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Nova/OpenTelemetry başlangıç ve kapanış hatalarını raporlamak
    /// için isteğe bağlı callback.
    ///
    /// Bu callback içinden tekrar Nova çağrısı yapılmamalıdır.
    /// </summary>
    public Action<string, Exception?>? DiagnosticHandler { get; set; }
}
using System;
using Microsoft.Extensions.Logging;
using Nova.Observability.Abstractions;

namespace Nova.Observability.File;

public sealed class NovaFileLoggingOptions
{
    public bool Enabled { get; set; } =
        true;

    public string DirectoryPath { get; set; } =
        "logs";

    public string FileNamePrefix { get; set; } =
        "nova";

    public NovaFileLogFormat Format { get; set; } =
        NovaFileLogFormat.JsonLines;

    public LogLevel MinimumLevel { get; set; } =
        LogLevel.Information;

    public int QueueCapacity { get; set; } =
        4096;

    public int MaxFileSizeMegabytes { get; set; } =
        50;

    public int RetentionDays { get; set; } =
        14;

    public int ShutdownTimeoutMilliseconds { get; set; } =
        3000;

    public string? ServiceName { get; set; }

    public string? EnvironmentName { get; set; }

    public bool UseLocalTime { get; set; } =
    true;

    public string TimestampFormat { get; set; } =
        "yyyy-MM-dd HH:mm:ss.fff";

    public bool UseShortCategoryName { get; set; } =
        true;

    public bool IncludeTraceId { get; set; } =
        true;

    public int TraceIdDisplayLength { get; set; } =
        8;

    public bool HighlightIdentifiers { get; set; } =
        true;

    public Action<string, Exception?>?
        DiagnosticHandler
    { get; set; }

    public NovaDataProtectionOptions DataProtection
    {
        get;
    } = new NovaDataProtectionOptions();
}
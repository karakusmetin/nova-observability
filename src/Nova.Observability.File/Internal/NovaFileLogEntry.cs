using System;
using System.Collections.Generic;

namespace Nova.Observability.File;

internal sealed class NovaFileLogEntry
{
    public DateTimeOffset TimestampUtc { get; set; }

    public string? Level { get; set; }

    public string? Category { get; set; }

    public int EventId { get; set; }

    public string? Message { get; set; }

    public string? TraceId { get; set; }

    public string? SpanId { get; set; }

    public string? ServiceName { get; set; }

    public string? EnvironmentName { get; set; }

    public string? MachineName { get; set; }

    public string? ExceptionType { get; set; }

    public string? ExceptionMessage { get; set; }

    public string? ExceptionStackTrace { get; set; }

    public IDictionary<string, object?> Properties
    {
        get;
        set;
    } =
        new Dictionary<string, object?>();
}
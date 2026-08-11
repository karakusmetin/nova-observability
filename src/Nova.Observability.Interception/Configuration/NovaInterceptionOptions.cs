using System;

namespace Nova.Observability.Interception;

public sealed class NovaInterceptionOptions
{
    public bool Enabled { get; set; } =
        true;

    public bool EnableLogScopes { get; set; } =
        true;

    public bool LogFailures { get; set; } =
        true;

    public Action<string, Exception?>? DiagnosticHandler
    {
        get;
        set;
    }
}
namespace Nova.Observability.Abstractions;

public static class TelemetryNames
{
    public const string InstrumentationName = "Nova.Observability";
    public const string InstrumentationVersion = "0.1.0";

    public const string OperationStepEventName =
        "nova.operation.step";

    public const string ExceptionEventName =
        "exception";

    public const string OperationStartedMetricName =
        "nova.operation.started";

    public const string OperationExecutionsMetricName =
        "nova.operation.executions";

    public const string OperationActiveMetricName =
        "nova.operation.active";

    public const string OperationDurationMetricName =
        "nova.operation.duration";

    public const string ServiceAliveMetricName =
    "nova.service.alive";

    public const string ServiceUptimeMetricName =
        "nova.service.uptime";

    public const string ServiceHeartbeatTimestampMetricName =
        "nova.service.heartbeat.timestamp";
}
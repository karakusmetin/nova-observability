namespace Nova.Observability.Abstractions;

public static class TelemetryTags
{
    public const string EventName =
        "nova.event.name";

    public const string DisplayMessage =
        "nova.display.message";

    public const string OperationId =
        "nova.operation.id";

    public const string OperationName =
        "nova.operation.name";

    public const string OperationKind =
        "nova.operation.kind";

    public const string OperationResult =
        "nova.operation.result";

    public const string OperationDomain =
        "nova.operation.domain";

    public const string OperationAction =
        "nova.operation.action";

    public const string OperationStepName =
        "nova.operation.step.name";

    public const string OperationDurationMilliseconds =
        "nova.operation.duration.ms";

    public const string EntityType =
        "nova.entity.type";

    public const string EntityId =
        "nova.entity.id";

    public const string CorrelationId =
        "nova.correlation.id";

    public const string CancellationReason =
        "nova.cancellation.reason";

    public const string ExceptionType =
        "exception.type";

    public const string ExceptionMessage =
        "exception.message";

    public const string ExceptionStackTrace =
        "exception.stacktrace";
}
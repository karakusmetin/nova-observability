using System.Collections.Generic;

namespace Nova.Observability.Abstractions;

public sealed class NovaOperationOptions
{
    public OperationKind Kind { get; set; } = OperationKind.Internal;

    public string? DisplayName { get; set; }

    public string? Domain { get; set; }

    public string? Action { get; set; }

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public string? CorrelationId { get; set; }

    public IEnumerable<KeyValuePair<string, object?>>? Tags { get; set; }
}
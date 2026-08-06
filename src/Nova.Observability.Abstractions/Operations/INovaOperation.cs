using System;
using System.Collections.Generic;

namespace Nova.Observability.Abstractions;

public interface INovaOperation : IDisposable
{
    string OperationId { get; }

    string? TraceId { get; }

    string? SpanId { get; }

    string CorrelationId { get; }

    bool IsCompleted { get; }

    void SetTag(
        string name,
        object? value);

    void Step(
        string stepName,
        string? displayMessage = null,
        IEnumerable<KeyValuePair<string, object?>>? tags = null);

    void Complete(
        IEnumerable<KeyValuePair<string, object?>>? tags = null);

    void Fail(
        Exception exception,
        IEnumerable<KeyValuePair<string, object?>>? tags = null);

    void Cancel(
        string? reason = null,
        IEnumerable<KeyValuePair<string, object?>>? tags = null);
}
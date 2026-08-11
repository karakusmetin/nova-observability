using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Nova.Observability.Abstractions;

namespace Nova.Observability.Core;

internal sealed class NovaOperation : INovaOperation
{
    private readonly string _operationName;
    private readonly NovaOperationOptions _options;
    private readonly Activity? _activity;
    private readonly Stopwatch _stopwatch;

    private readonly KeyValuePair<string, object?>[]
        _baseMetricTags;

    private int _state;

    internal NovaOperation(
        string operationName,
        NovaOperationOptions options,
        ActivityContext? explicitParentContext = null)
    {
        _operationName = operationName;
        _options = options;

        OperationId = Guid.NewGuid().ToString("N");

        _stopwatch = Stopwatch.StartNew();

        var parentActivity = Activity.Current;

        if (explicitParentContext.HasValue)
        {
            _activity =
                NovaTelemetry.ActivitySource.StartActivity(
                    operationName,
                    MapActivityKind(options.Kind),
                    explicitParentContext.Value);
        }
        else
        {
            _activity =
                NovaTelemetry.ActivitySource.StartActivity(
                    operationName,
                    MapActivityKind(options.Kind));
        }

        if (_activity != null &&
            !string.IsNullOrWhiteSpace(options.DisplayName))
        {
            _activity.DisplayName = options.DisplayName;
        }

        TraceId =
            _activity?.TraceId.ToString()
            ?? GetTraceId(explicitParentContext)
            ?? parentActivity?.TraceId.ToString();

        SpanId =
            _activity?.SpanId.ToString();

        var inheritedCorrelationId =
            parentActivity?
                .GetTagItem(TelemetryTags.CorrelationId)?
                .ToString();

        CorrelationId =
            !string.IsNullOrWhiteSpace(options.CorrelationId)
                ? options.CorrelationId!
                : !string.IsNullOrWhiteSpace(inheritedCorrelationId)
                    ? inheritedCorrelationId!
                    : !string.IsNullOrWhiteSpace(TraceId)
                        ? TraceId!
                        : OperationId;

        ApplyInitialActivityTags();

        TelemetryTagHelper.Apply(
            _activity,
            options.Tags);

        _baseMetricTags =
            CreateMetricTags(result: null);

        NovaTelemetry.OperationStarted.Add(
            1,
            _baseMetricTags);

        NovaTelemetry.ActiveOperations.Add(
            1,
            _baseMetricTags);
    }

    public string OperationId { get; }

    public string? TraceId { get; }

    public string? SpanId { get; }

    public string CorrelationId { get; }

    public bool IsCompleted =>
        Volatile.Read(ref _state) != 0;

    public void SetTag(
        string name,
        object? value)
    {
        if (Volatile.Read(ref _state) != 0)
            return;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Tag name cannot be empty.",
                nameof(name));
        }

        _activity?.SetTag(
        name,
        NovaTelemetry.ProtectAttribute(
            name,
            value));
    }

    public void Step(
        string stepName,
        string? displayMessage = null,
        IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        if (Volatile.Read(ref _state) != 0)
            return;

        if (string.IsNullOrWhiteSpace(stepName))
        {
            throw new ArgumentException(
                "Step name cannot be empty.",
                nameof(stepName));
        }

        if (_activity == null)
            return;

        var eventTags =
            TelemetryTagHelper.CreateActivityTags(tags);

        eventTags[TelemetryTags.EventName] =
            TelemetryNames.OperationStepEventName;

        eventTags[TelemetryTags.OperationStepName] =
            stepName;

        if (!string.IsNullOrWhiteSpace(displayMessage))
        {
            eventTags[TelemetryTags.DisplayMessage] =
                displayMessage;
        }

        _activity.AddEvent(
            new ActivityEvent(
                TelemetryNames.OperationStepEventName,
                default(DateTimeOffset),
                eventTags));
    }

    public void Complete(
        IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        Finish(
            OperationResult.Success,
            exception: null,
            reason: null,
            tags);
    }

    public void Fail(
        Exception exception,
        IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        Finish(
            OperationResult.Failure,
            exception,
            reason: null,
            tags);
    }

    public void Cancel(
        string? reason = null,
        IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        Finish(
            OperationResult.Cancelled,
            exception: null,
            reason,
            tags);
    }

    public void Dispose()
    {
        // Complete veya Fail çağrılmadan scope kapatılmışsa
        // yanlışlıkla başarılı saymıyoruz.
        Finish(
            OperationResult.Unset,
            exception: null,
            reason: null,
            tags: null);
    }

    private void Finish(
        OperationResult result,
        Exception? exception,
        string? reason,
        IEnumerable<KeyValuePair<string, object?>>? tags)
    {
        if (Interlocked.CompareExchange(
                ref _state,
                1,
                0) != 0)
        {
            return;
        }

        _stopwatch.Stop();

        try
        {
            TelemetryTagHelper.Apply(
                _activity,
                tags);

            _activity?.SetTag(
                TelemetryTags.OperationResult,
                ToTagValue(result));

            _activity?.SetTag(
                TelemetryTags.OperationDurationMilliseconds,
                _stopwatch.Elapsed.TotalMilliseconds);

            if (!string.IsNullOrWhiteSpace(reason))
            {
                _activity?.SetTag(
                    TelemetryTags.CancellationReason,
                    reason);
            }

            SetActivityStatus(
                result,
                exception);

            if (exception != null)
            {
                AddExceptionEvent(exception);
            }

            var finalMetricTags =
                CreateMetricTags(result);

            NovaTelemetry.OperationExecutions.Add(
                1,
                finalMetricTags);

            NovaTelemetry.OperationDuration.Record(
                _stopwatch.Elapsed.TotalSeconds,
                finalMetricTags);
        }
        finally
        {
            NovaTelemetry.ActiveOperations.Add(
                -1,
                _baseMetricTags);

            _activity?.Dispose();
        }
    }

    private void ApplyInitialActivityTags()
    {
        if (_activity == null)
            return;

        _activity.SetTag(
            TelemetryTags.OperationId,
            OperationId);

        _activity.SetTag(
            TelemetryTags.OperationName,
            _operationName);

        _activity.SetTag(
            TelemetryTags.OperationKind,
            ToTagValue(_options.Kind));

        _activity.SetTag(
            TelemetryTags.CorrelationId,
            CorrelationId);

        SetOptionalTag(
            TelemetryTags.DisplayMessage,
            _options.DisplayName);

        SetOptionalTag(
            TelemetryTags.OperationDomain,
            _options.Domain);

        SetOptionalTag(
            TelemetryTags.OperationAction,
            _options.Action);

        SetOptionalTag(
            TelemetryTags.EntityType,
            _options.EntityType);

        SetOptionalTag(
            TelemetryTags.EntityId,
            _options.EntityId);
    }

    private void AddExceptionEvent(
        Exception exception)
    {
        if (_activity == null)
            return;

        var exceptionTags =
            new ActivityTagsCollection();

        exceptionTags[
            TelemetryTags.ExceptionType] =
                exception.GetType().FullName
                ?? exception.GetType().Name;

        exceptionTags[
            TelemetryTags.ExceptionMessage] =
                NovaTelemetry
                    .ProtectExceptionMessage(
                        exception.Message);

        exceptionTags[
            TelemetryTags.ExceptionStackTrace] =
                NovaTelemetry
                    .ProtectExceptionStackTrace(
                        exception);

        _activity.AddEvent(
            new ActivityEvent(
                TelemetryNames.ExceptionEventName,
                default(DateTimeOffset),
                exceptionTags));
    }

    private void SetActivityStatus(
        OperationResult result,
        Exception? exception)
    {
        if (_activity == null)
            return;

        switch (result)
        {
            case OperationResult.Success:
                _activity.SetStatus(
                    ActivityStatusCode.Ok);
                break;

            case OperationResult.Failure:
                _activity.SetStatus(
                    ActivityStatusCode.Error,
                    exception?.Message);
                break;

            case OperationResult.Cancelled:
            case OperationResult.Unset:
            default:
                _activity.SetStatus(
                    ActivityStatusCode.Unset);
                break;
        }
    }

    private KeyValuePair<string, object?>[]
        CreateMetricTags(OperationResult? result)
    {
        var tags =
            new List<KeyValuePair<string, object?>>(5)
            {
                new(
                    TelemetryTags.OperationName,
                    _operationName),

                new(
                    TelemetryTags.OperationKind,
                    ToTagValue(_options.Kind))
            };

        if (!string.IsNullOrWhiteSpace(_options.Domain))
        {
            tags.Add(
                new KeyValuePair<string, object?>(
                    TelemetryTags.OperationDomain,
                    _options.Domain));
        }

        if (!string.IsNullOrWhiteSpace(_options.Action))
        {
            tags.Add(
                new KeyValuePair<string, object?>(
                    TelemetryTags.OperationAction,
                    _options.Action));
        }

        if (result.HasValue)
        {
            tags.Add(
                new KeyValuePair<string, object?>(
                    TelemetryTags.OperationResult,
                    ToTagValue(result.Value)));
        }

        return tags.ToArray();
    }

    private void SetOptionalTag(
        string name,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        _activity?.SetTag(
            name,
            value);
    }

    private static ActivityKind MapActivityKind(
        OperationKind kind)
    {
        return kind switch
        {
            OperationKind.Server =>
                ActivityKind.Server,

            OperationKind.Client =>
                ActivityKind.Client,

            OperationKind.Producer =>
                ActivityKind.Producer,

            OperationKind.Consumer =>
                ActivityKind.Consumer,

            _ =>
                ActivityKind.Internal
        };
    }

    private static string ToTagValue(
        OperationKind kind)
    {
        return kind
            .ToString()
            .ToLowerInvariant();
    }

    private static string ToTagValue(
        OperationResult result)
    {
        return result
            .ToString()
            .ToLowerInvariant();
    }
    private static string? GetTraceId(
    ActivityContext? context)
    {
        if (!context.HasValue)
            return null;

        if (context.Value.TraceId ==
            default(ActivityTraceId))
        {
            return null;
        }

        return context
            .Value
            .TraceId
            .ToString();
    }
}
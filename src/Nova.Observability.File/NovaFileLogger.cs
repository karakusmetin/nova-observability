using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Nova.Observability.File;

internal sealed class NovaFileLogger :
    ILogger
{
    private readonly string
        _categoryName;

    private readonly NovaFileLoggerProvider
        _provider;

    internal NovaFileLogger(
        string categoryName,
        NovaFileLoggerProvider provider)
    {
        _categoryName =
            categoryName;

        _provider =
            provider;
    }

    public IDisposable BeginScope<TState>(
        TState state)
        where TState : notnull
    {
        return _provider
            .ScopeProvider
            .Push(
                state);
    }

    public bool IsEnabled(
        LogLevel logLevel)
    {
        if (!_provider.Options.Enabled)
            return false;

        if (logLevel ==
            LogLevel.None)
        {
            return false;
        }

        return logLevel >=
               _provider.Options.MinimumLevel;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string>
            formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        try
        {
            var message =
                formatter(
                    state,
                    exception);

            var activity =
                Activity.Current;

            var entry =
                new NovaFileLogEntry
                {
                    TimestampUtc =
                        DateTimeOffset.UtcNow,

                    Level =
                        logLevel.ToString(),

                    Category =
                        _categoryName,

                    EventId =
                        eventId.Id,

                    Message =
                        _provider
                            .Sanitizer
                            .ProtectLogMessage(
                                message),

                    TraceId =
                        activity?
                            .TraceId
                            .ToString(),

                    SpanId =
                        activity?
                            .SpanId
                            .ToString(),

                    ServiceName =
                        _provider
                            .Options
                            .ServiceName,

                    EnvironmentName =
                        _provider
                            .Options
                            .EnvironmentName,

                    MachineName =
                        Environment.MachineName,

                    ExceptionType =
                        exception?
                            .GetType()
                            .FullName,

                    ExceptionMessage =
                        _provider
                            .Sanitizer
                            .ProtectExceptionMessage(
                                exception?
                                    .Message),

                    ExceptionStackTrace =
                        _provider
                            .Sanitizer
                            .ProtectExceptionStackTrace(
                                exception)
                };

            CaptureState(
                state,
                entry.Properties);

            CaptureScopes(
                entry.Properties);

            _provider.TryWrite(
                entry);
        }
        catch
        {
            /*
             * File logging application
             * davranışını etkileyemez.
             */
        }
    }

    private void CaptureState<TState>(
        TState state,
        IDictionary<string, object?>
            properties)
    {
        var values =
            state as IEnumerable<
                KeyValuePair<
                    string,
                    object?>>;

        if (values == null)
            return;

        foreach (var value in values)
        {
            if (value.Key ==
                "{OriginalFormat}")
            {
                continue;
            }

            properties[value.Key] =
                _provider
                    .Sanitizer
                    .ProtectAttribute(
                        value.Key,
                        value.Value);
        }
    }

    private void CaptureScopes(
        IDictionary<string, object?>
            properties)
    {
        var scopeIndex =
            0;

        _provider
            .ScopeProvider
            .ForEachScope(
                delegate (
                    object scope,
                    object state)
                {
                    var target =
                        (IDictionary<
                            string,
                            object?>)state;

                    var values =
                        scope as IEnumerable<
                            KeyValuePair<
                                string,
                                object?>>;

                    if (values != null)
                    {
                        foreach (
                            var value
                            in values)
                        {
                            target[value.Key] =
                                _provider
                                    .Sanitizer
                                    .ProtectAttribute(
                                        value.Key,
                                        value.Value);
                        }

                        return;
                    }

                    target[
                        "scope." +
                        scopeIndex++] =
                            _provider
                                .Sanitizer
                                .ProtectAttribute(
                                    "scope",
                                    scope);
                },
                properties);
    }
}
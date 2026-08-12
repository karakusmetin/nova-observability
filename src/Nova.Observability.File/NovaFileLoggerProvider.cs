using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using Nova.Observability.Core;

namespace Nova.Observability.File;

public sealed class NovaFileLoggerProvider :
    ILoggerProvider,
    ISupportExternalScope
{
    private readonly NovaFileLoggingOptions
        _options;

    private readonly BlockingCollection<
        NovaFileLogEntry> _queue;

    private readonly NovaFileLogWriter
        _writer;

    private readonly NovaTelemetrySanitizer
        _sanitizer;

    private readonly Thread
        _workerThread;

    private IExternalScopeProvider
        _scopeProvider;

    private int _disposed;

    private long _droppedLogs;

    public NovaFileLoggerProvider(
        NovaFileLoggingOptions options)
    {
        _options =
            options
            ?? throw new ArgumentNullException(
                nameof(options));

        var capacity =
            options.QueueCapacity > 0
                ? options.QueueCapacity
                : 4096;

        _queue =
            new BlockingCollection<
                NovaFileLogEntry>(
                    capacity);

        _writer =
            new NovaFileLogWriter(
                options);

        _sanitizer =
            new NovaTelemetrySanitizer(
                options.DataProtection);

        _scopeProvider =
            new LoggerExternalScopeProvider();

        _workerThread =
            new Thread(
                ProcessQueue);

        _workerThread.IsBackground =
            true;

        _workerThread.Name =
            "NovaFileLogger";

        _workerThread.Start();
    }

    internal IExternalScopeProvider ScopeProvider =>
        _scopeProvider;

    internal NovaTelemetrySanitizer Sanitizer =>
        _sanitizer;

    internal NovaFileLoggingOptions Options =>
        _options;

    public ILogger CreateLogger(
        string categoryName)
    {
        return new NovaFileLogger(
            categoryName,
            this);
    }

    public void SetScopeProvider(
        IExternalScopeProvider scopeProvider)
    {
        _scopeProvider =
            scopeProvider
            ?? new LoggerExternalScopeProvider();
    }

    internal bool TryWrite(
        NovaFileLogEntry entry)
    {
        if (Volatile.Read(
                ref _disposed) != 0)
        {
            return false;
        }

        try
        {
            if (_queue.TryAdd(
                    entry))
            {
                return true;
            }

            Interlocked.Increment(
                ref _droppedLogs);

            ReportSafely(
                "Nova file log queue dolu. " +
                "Log kaydı düşürüldü.",
                null);

            return false;
        }
        catch (Exception exception)
        {
            ReportSafely(
                "Nova file log kuyruğuna kayıt eklenemedi.",
                exception);

            return false;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(
                ref _disposed,
                1) != 0)
        {
            return;
        }

        try
        {
            _queue.CompleteAdding();
        }
        catch
        {
        }

        try
        {
            _workerThread.Join(
                _options
                    .ShutdownTimeoutMilliseconds);
        }
        catch
        {
        }

        try
        {
            _writer.Dispose();
        }
        catch
        {
        }

        try
        {
            _queue.Dispose();
        }
        catch
        {
        }
    }

    private void ProcessQueue()
    {
        try
        {
            foreach (var entry in
                     _queue.GetConsumingEnumerable())
            {
                try
                {
                    _writer.Write(
                        entry);
                }
                catch (Exception exception)
                {
                    ReportSafely(
                        "Nova fiziksel log dosyasına yazamadı.",
                        exception);
                }
            }
        }
        catch (Exception exception)
        {
            ReportSafely(
                "Nova file logging background worker hata aldı.",
                exception);
        }
    }

    private void ReportSafely(
        string message,
        Exception? exception)
    {
        var handler =
            _options.DiagnosticHandler;

        if (handler == null)
            return;

        try
        {
            handler(
                message,
                exception);
        }
        catch
        {
        }
    }
}
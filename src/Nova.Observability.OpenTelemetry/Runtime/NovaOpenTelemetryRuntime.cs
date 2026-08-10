using System;
using System.Threading;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Nova.Observability.OpenTelemetry;

public sealed class NovaOpenTelemetryRuntime : IDisposable
{
    private TracerProvider? _tracerProvider;
    private MeterProvider? _meterProvider;

    private readonly NovaOpenTelemetryOptions _options;

    private int _disposed;

    internal NovaOpenTelemetryRuntime(
        NovaOpenTelemetryOptions options,
        TracerProvider? tracerProvider,
        MeterProvider? meterProvider,
        Exception? initializationException)
    {
        _options = options;
        _tracerProvider = tracerProvider;
        _meterProvider = meterProvider;

        InitializationException = initializationException;

        IsEnabled =
            tracerProvider != null ||
            meterProvider != null;
    }

    public bool IsEnabled { get; }

    public Exception? InitializationException { get; }

    public bool IsDisposed =>
        Volatile.Read(ref _disposed) != 0;

    public bool TryForceFlush()
    {
        return TryForceFlush(
            _options.ShutdownFlushTimeoutMilliseconds);
    }

    public bool TryForceFlush(
        int timeoutMilliseconds)
    {
        if (IsDisposed)
            return false;

        if (timeoutMilliseconds <= 0)
        {
            timeoutMilliseconds =
                _options.ShutdownFlushTimeoutMilliseconds;
        }

        try
        {
            var traceResult =
                _tracerProvider?.ForceFlush(timeoutMilliseconds)
                ?? true;

            var metricResult =
                _meterProvider?.ForceFlush(timeoutMilliseconds)
                ?? true;

            return traceResult && metricResult;
        }
        catch (Exception exception)
        {
            NovaOpenTelemetryDiagnostics.Report(
                _options,
                "Telemetry verileri flush edilirken hata oluştu.",
                exception);

            return false;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        // Önce mümkün olduğu kadar bekleyen veriyi gönder.
        TryForceFlushBeforeDispose();

        var meterProvider =
            Interlocked.Exchange(
                ref _meterProvider,
                null);

        var tracerProvider =
            Interlocked.Exchange(
                ref _tracerProvider,
                null);

        NovaOpenTelemetryDiagnostics.DisposeSafely(
            meterProvider,
            _options,
            nameof(MeterProvider));

        NovaOpenTelemetryDiagnostics.DisposeSafely(
            tracerProvider,
            _options,
            nameof(TracerProvider));
    }

    private void TryForceFlushBeforeDispose()
    {
        try
        {
            var timeout =
                _options.ShutdownFlushTimeoutMilliseconds;

            _tracerProvider?.ForceFlush(timeout);
            _meterProvider?.ForceFlush(timeout);
        }
        catch (Exception exception)
        {
            NovaOpenTelemetryDiagnostics.Report(
                _options,
                "Telemetry kapanış flush işlemi başarısız oldu.",
                exception);
        }
    }
}
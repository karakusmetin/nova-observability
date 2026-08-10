using System;

namespace Nova.Observability.OpenTelemetry;

internal static class NovaOpenTelemetryDiagnostics
{
    internal static void Report(
        NovaOpenTelemetryOptions options,
        string message,
        Exception? exception = null)
    {
        var handler = options.DiagnosticHandler;

        if (handler == null)
            return;

        try
        {
            handler(message, exception);
        }
        catch
        {
            // Diagnostic callback hiçbir koşulda uygulamanın
            // business akışına exception taşımamalıdır.
        }
    }

    internal static void DisposeSafely(
        IDisposable? disposable,
        NovaOpenTelemetryOptions options,
        string componentName)
    {
        if (disposable == null)
            return;

        try
        {
            disposable.Dispose();
        }
        catch (Exception exception)
        {
            Report(
                options,
                componentName + " kapatılırken hata oluştu.",
                exception);
        }
    }
}
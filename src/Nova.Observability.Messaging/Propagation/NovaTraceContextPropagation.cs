using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using OpenTelemetry.Context.Propagation;

namespace Nova.Observability.Messaging;

public static class NovaTraceContextPropagation
{
    private static readonly TextMapPropagator Propagator =
        new TraceContextPropagator();

    public static bool TryInjectCurrentContext(
        IDictionary<string, object?> headers,
        Action<string, Exception?>? diagnosticHandler = null)
    {
        if (headers == null)
            throw new ArgumentNullException(nameof(headers));

        var activity =
            Activity.Current;

        if (activity == null)
            return false;

        try
        {
            var context =
                new PropagationContext(
                    activity.Context,
                    default);

            Propagator.Inject(
                context,
                headers,
                Inject);

            return true;
        }
        catch (Exception exception)
        {
            ReportSafely(
                diagnosticHandler,
                "Trace context message header'larına yazılamadı.",
                exception);

            return false;
        }
    }

    public static bool TryExtractParentContext(
        IDictionary<string, object?>? headers,
        out ActivityContext parentContext,
        Action<string, Exception?>? diagnosticHandler = null)
    {
        parentContext =
            default;

        if (headers == null ||
            headers.Count == 0)
        {
            return false;
        }

        try
        {
            var propagationContext =
                Propagator.Extract(
                    default,
                    headers,
                    Extract);

            var extracted =
                propagationContext.ActivityContext;

            if (extracted.TraceId ==
                default(ActivityTraceId))
            {
                return false;
            }

            parentContext =
                extracted;

            return true;
        }
        catch (Exception exception)
        {
            ReportSafely(
                diagnosticHandler,
                "Trace context message header'larından okunamadı.",
                exception);

            return false;
        }
    }

    private static void Inject(
        IDictionary<string, object?> carrier,
        string key,
        string value)
    {
        /*
         * RabbitMQ header değerleri broker'dan
         * döndüğünde byte[] olarak gelebilir.
         *
         * Baştan UTF8 byte[] kullanarak davranışı
         * deterministik hale getiriyoruz.
         */
        carrier[key] =
            Encoding.UTF8.GetBytes(
                value);
    }

    private static IEnumerable<string> Extract(
        IDictionary<string, object?> carrier,
        string key)
    {
        if (!carrier.TryGetValue(
                key,
                out var value) ||
            value == null)
        {
            return Array.Empty<string>();
        }

        if (value is string text)
        {
            return new[]
            {
                text
            };
        }

        if (value is byte[] bytes)
        {
            return new[]
            {
                Encoding.UTF8.GetString(
                    bytes)
            };
        }

        if (value is ReadOnlyMemory<byte> memory)
        {
            return new[]
            {
                Encoding.UTF8.GetString(
                    memory.ToArray())
            };
        }

        return Array.Empty<string>();
    }

    private static void ReportSafely(
        Action<string, Exception?>? handler,
        string message,
        Exception? exception)
    {
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
            /*
             * Propagation diagnostic mekanizması
             * business akışını etkileyemez.
             */
        }
    }
}
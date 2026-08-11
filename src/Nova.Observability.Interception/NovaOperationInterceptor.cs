using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Nova.Observability.Abstractions;
using Nova.Observability.Core;

namespace Nova.Observability.Interception;

public sealed class NovaOperationInterceptor :
    IInterceptor
{
    private readonly NovaInterceptionOptions
        _options;

    private readonly ILoggerFactory?
        _loggerFactory;

    public NovaOperationInterceptor(
        NovaInterceptionOptions options,
        ILoggerFactory? loggerFactory = null)
    {
        _options =
            options
            ?? throw new ArgumentNullException(
                nameof(options));

        _loggerFactory =
            loggerFactory;
    }

    public void Intercept(
        IInvocation invocation)
    {
        if (!_options.Enabled)
        {
            invocation.Proceed();
            return;
        }

        if (!OperationObservationResolver.TryResolve(
                invocation,
                out var descriptor,
                out var resolutionError))
        {
            if (!string.IsNullOrWhiteSpace(
                    resolutionError))
            {
                ReportDiagnostic(
                    resolutionError,
                    null);
            }

            invocation.Proceed();
            return;
        }

        if (descriptor == null)
        {
            invocation.Proceed();
            return;
        }

        var returnType =
            invocation.Method.ReturnType;

        if (returnType == typeof(Task))
        {
            invocation.ReturnValue =
                InterceptTaskAsync(
                    invocation,
                    descriptor);

            return;
        }

        if (returnType.IsGenericType &&
            returnType.GetGenericTypeDefinition()
                == typeof(Task<>))
        {
            invocation.ReturnValue =
                CreateGenericTask(
                    invocation,
                    descriptor,
                    returnType);

            return;
        }

        /*
         * ValueTask desteğini yanlış şekilde
         * tamamlandı saymaktansa şimdilik
         * instrumentation dışı bırakıyoruz.
         *
         * Ayrı committe ekleyeceğiz.
         */
        if (IsValueTask(returnType))
        {
            ReportDiagnostic(
                $"ValueTask henüz desteklenmiyor. " +
                $"Operation gözlemlenmeden çalıştırılacak: " +
                $"{descriptor.Attribute.Name}",
                null);

            invocation.Proceed();
            return;
        }

        InterceptSynchronously(
            invocation,
            descriptor);
    }

    private void InterceptSynchronously(
        IInvocation invocation,
        OperationObservationDescriptor descriptor)
    {
        var operation =
            TryStartOperation(
                invocation,
                descriptor);

        var logger =
            TryCreateLogger(invocation);

        var logScope =
            TryBeginLogScope(
                logger,
                operation,
                descriptor,
                invocation.Arguments);

        try
        {
            invocation.Proceed();

            SafeComplete(
                operation);
        }
        catch (OperationCanceledException)
        {
            SafeCancel(
                operation);

            throw;
        }
        catch (Exception exception)
        {
            SafeLogFailure(
                logger,
                descriptor,
                exception);

            SafeFail(
                operation,
                exception);

            throw;
        }
        finally
        {
            SafeDispose(
                logScope);

            SafeDispose(
                operation);
        }
    }

    private async Task InterceptTaskAsync(
        IInvocation invocation,
        OperationObservationDescriptor descriptor)
    {
        var operation =
            TryStartOperation(
                invocation,
                descriptor);

        var logger =
            TryCreateLogger(invocation);

        var logScope =
            TryBeginLogScope(
                logger,
                operation,
                descriptor,
                invocation.Arguments);

        try
        {
            invocation.Proceed();

            var task =
                invocation.ReturnValue as Task;

            if (task == null)
            {
                ReportDiagnostic(
                    "Intercept edilen method Task döndürmesi gerekirken null döndürdü.",
                    null);

                return;
            }

            await task.ConfigureAwait(false);

            SafeComplete(
                operation);
        }
        catch (OperationCanceledException)
        {
            SafeCancel(
                operation);

            throw;
        }
        catch (Exception exception)
        {
            SafeLogFailure(
                logger,
                descriptor,
                exception);

            SafeFail(
                operation,
                exception);

            throw;
        }
        finally
        {
            SafeDispose(
                logScope);

            SafeDispose(
                operation);
        }
    }

    private async Task<TResult>
        InterceptTaskWithResultAsync<TResult>(
            IInvocation invocation,
            OperationObservationDescriptor descriptor)
    {
        var operation =
            TryStartOperation(
                invocation,
                descriptor);

        var logger =
            TryCreateLogger(invocation);

        var logScope =
            TryBeginLogScope(
                logger,
                operation,
                descriptor,
                invocation.Arguments);

        try
        {
            invocation.Proceed();

            var task =
                invocation.ReturnValue
                    as Task<TResult>;

            if (task == null)
            {
                throw new InvalidOperationException(
                    "Intercept edilen Task<TResult> " +
                    "beklenen türde bir Task döndürmedi.");
            }

            var result =
                await task.ConfigureAwait(false);

            SafeComplete(
                operation);

            return result;
        }
        catch (OperationCanceledException)
        {
            SafeCancel(
                operation);

            throw;
        }
        catch (Exception exception)
        {
            SafeLogFailure(
                logger,
                descriptor,
                exception);

            SafeFail(
                operation,
                exception);

            throw;
        }
        finally
        {
            SafeDispose(
                logScope);

            SafeDispose(
                operation);
        }
    }

    private object? CreateGenericTask(
        IInvocation invocation,
        OperationObservationDescriptor descriptor,
        Type returnType)
    {
        try
        {
            var resultType =
                returnType
                    .GetGenericArguments()[0];

            var method =
                typeof(NovaOperationInterceptor)
                    .GetMethod(
                        nameof(
                            InterceptTaskWithResultAsync),
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

            if (method == null)
            {
                ReportDiagnostic(
                    "Task<TResult> interception helper bulunamadı.",
                    null);

                invocation.Proceed();

                return invocation.ReturnValue;
            }

            var genericMethod =
                method.MakeGenericMethod(
                    resultType);

            return genericMethod.Invoke(
                this,
                new object[]
                {
                    invocation,
                    descriptor
                });
        }
        catch (Exception exception)
        {
            /*
             * Proxy altyapısındaki bir hata yüzünden
             * business metodunu çalıştırmamak istemiyoruz.
             */
            ReportDiagnostic(
                "Task<TResult> interception oluşturulamadı. " +
                "Method gözlemlenmeden çalıştırılacak.",
                exception);

            invocation.Proceed();

            return invocation.ReturnValue;
        }
    }

    private INovaOperation? TryStartOperation(
        IInvocation invocation,
        OperationObservationDescriptor descriptor)
    {
        try
        {
            var operationOptions =
                descriptor.CreateOptions(
                    invocation.Arguments);

            return NovaTelemetry.StartOperation(
                descriptor.Attribute.Name,
                operationOptions);
        }
        catch (Exception exception)
        {
            ReportDiagnostic(
                "Nova operation başlatılamadı. " +
                "Business method çalışmaya devam edecek.",
                exception);

            return null;
        }
    }

    private ILogger? TryCreateLogger(
        IInvocation invocation)
    {
        if (_loggerFactory == null)
            return null;

        try
        {
            var categoryName =
                invocation.InvocationTarget?
                    .GetType()
                    .FullName
                ?? invocation.Method
                    .DeclaringType?
                    .FullName
                ?? "Nova.ObservedOperation";

            return _loggerFactory.CreateLogger(
                categoryName);
        }
        catch
        {
            return null;
        }
    }

    private IDisposable? TryBeginLogScope(
        ILogger? logger,
        INovaOperation? operation,
        OperationObservationDescriptor descriptor,
        object?[] arguments)
    {
        if (!_options.EnableLogScopes ||
            logger == null ||
            operation == null)
        {
            return null;
        }

        try
        {
            var operationOptions =
                descriptor.CreateOptions(
                    arguments);

            var values =
                new Dictionary<string, object?>
                {
                    [TelemetryTags.OperationName] =
                        descriptor.Attribute.Name,

                    [TelemetryTags.CorrelationId] =
                        operation.CorrelationId
                };

            if (!string.IsNullOrWhiteSpace(
                    operationOptions.EntityType))
            {
                values[
                    TelemetryTags.EntityType] =
                        operationOptions.EntityType;
            }

            if (!string.IsNullOrWhiteSpace(
                    operationOptions.EntityId))
            {
                values[
                    TelemetryTags.EntityId] =
                        operationOptions.EntityId;
            }

            return logger.BeginScope(
                values);
        }
        catch (Exception exception)
        {
            ReportDiagnostic(
                "Nova log scope oluşturulamadı.",
                exception);

            return null;
        }
    }

    private void SafeComplete(
        INovaOperation? operation)
    {
        if (operation == null)
            return;

        try
        {
            operation.Complete();
        }
        catch (Exception exception)
        {
            ReportDiagnostic(
                "Nova operation tamamlanırken hata oluştu.",
                exception);
        }
    }

    private void SafeFail(
        INovaOperation? operation,
        Exception businessException)
    {
        if (operation == null)
            return;

        try
        {
            operation.Fail(
                businessException);
        }
        catch (Exception exception)
        {
            ReportDiagnostic(
                "Nova failure telemetry üretilemedi.",
                exception);
        }
    }

    private void SafeCancel(
        INovaOperation? operation)
    {
        if (operation == null)
            return;

        try
        {
            operation.Cancel(
                "Operation cancelled.");
        }
        catch (Exception exception)
        {
            ReportDiagnostic(
                "Nova cancellation telemetry üretilemedi.",
                exception);
        }
    }

    private void SafeLogFailure(
        ILogger? logger,
        OperationObservationDescriptor descriptor,
        Exception exception)
    {
        if (!_options.LogFailures ||
            logger == null)
        {
            return;
        }

        try
        {
            logger.LogError(
                exception,
                "{OperationDisplayName} başarısız oldu. OperationName={OperationName}",
                descriptor.Attribute.DisplayName
                    ?? descriptor.Attribute.Name,
                descriptor.Attribute.Name);
        }
        catch (Exception loggingException)
        {
            ReportDiagnostic(
                "Observed operation hata logu yazılamadı.",
                loggingException);
        }
    }

    private void SafeDispose(
        IDisposable? disposable)
    {
        if (disposable == null)
            return;

        try
        {
            disposable.Dispose();
        }
        catch (Exception exception)
        {
            ReportDiagnostic(
                "Nova telemetry scope kapatılırken hata oluştu.",
                exception);
        }
    }

    private void ReportDiagnostic(
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
            // Diagnostic mekanizması business
            // uygulamasını etkileyemez.
        }
    }

    private static bool IsValueTask(
        Type returnType)
    {
        var fullName =
            returnType.FullName;

        return fullName ==
               "System.Threading.Tasks.ValueTask"
               ||
               (returnType.IsGenericType &&
                returnType
                    .GetGenericTypeDefinition()
                    .FullName ==
                "System.Threading.Tasks.ValueTask`1");
    }
}
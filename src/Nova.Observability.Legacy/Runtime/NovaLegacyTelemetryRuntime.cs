using System;
using System.Threading;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Nova.Observability.Interception;
using Nova.Observability.OpenTelemetry;

namespace Nova.Observability.Legacy
{
    public sealed class NovaLegacyTelemetryRuntime : IDisposable
    {
        private NovaOpenTelemetryRuntime _telemetryRuntime;
        private ILoggerFactory _loggerFactory;

        private readonly ProxyGenerator _proxyGenerator;
        private readonly NovaInterceptionOptions _interceptionOptions;
        private readonly Action<string, Exception> _diagnosticHandler;

        private int _disposed;

        internal NovaLegacyTelemetryRuntime(
            NovaOpenTelemetryRuntime telemetryRuntime,
            ILoggerFactory loggerFactory,
            NovaInterceptionOptions interceptionOptions,
            Action<string, Exception> diagnosticHandler)
        {
            _telemetryRuntime = telemetryRuntime;
            _loggerFactory = loggerFactory;
            _interceptionOptions = interceptionOptions;
            _diagnosticHandler = diagnosticHandler;

            _proxyGenerator = new ProxyGenerator();
        }

        public bool IsEnabled
        {
            get
            {
                return _telemetryRuntime != null &&
                       _telemetryRuntime.IsEnabled;
            }
        }

        public bool IsDisposed
        {
            get
            {
                return Volatile.Read(ref _disposed) != 0;
            }
        }

        public ILogger CreateLogger(
            string categoryName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(categoryName))
                throw new ArgumentException(
                    "Category name cannot be empty.",
                    "categoryName");

            return _loggerFactory.CreateLogger(
                categoryName);
        }

        public ILogger<T> CreateLogger<T>()
        {
            ThrowIfDisposed();

            return _loggerFactory.CreateLogger<T>();
        }

        public TService CreateObserved<TService>(
            TService target)
            where TService : class
        {
            ThrowIfDisposed();

            if (target == null)
                throw new ArgumentNullException("target");

            if (!typeof(TService).IsInterface)
            {
                throw new InvalidOperationException(
                    typeof(TService).FullName +
                    " bir interface olmalıdır.");
            }

            /*
             * Telemetry devre dışıysa proxy kurmak
             * zorunda değiliz.
             */
            if (!IsEnabled)
                return target;

            try
            {
                var interceptor =
                    new NovaOperationInterceptor(
                        _interceptionOptions,
                        _loggerFactory);

                return _proxyGenerator
                    .CreateInterfaceProxyWithTarget<TService>(
                        target,
                        interceptor);
            }
            catch (Exception exception)
            {
                ReportSafely(
                    "Legacy Nova proxy oluşturulamadı. " +
                    "Gerçek business nesnesi kullanılacak.",
                    exception);

                /*
                 * Fail-open:
                 * instrumentation problemi business
                 * service'i engellemez.
                 */
                return target;
            }
        }

        public bool TryForceFlush()
        {
            if (IsDisposed)
                return false;

            try
            {
                return _telemetryRuntime == null ||
                       _telemetryRuntime.TryForceFlush();
            }
            catch (Exception exception)
            {
                ReportSafely(
                    "Legacy telemetry flush başarısız oldu.",
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

            var loggerFactory =
                Interlocked.Exchange(
                    ref _loggerFactory,
                    null);

            var telemetryRuntime =
                Interlocked.Exchange(
                    ref _telemetryRuntime,
                    null);

            /*
             * Önce log pipeline.
             * LoggerFactory Dispose batch logları
             * flush etme fırsatı verir.
             */
            DisposeSafely(
                loggerFactory,
                "ILoggerFactory");

            DisposeSafely(
                telemetryRuntime,
                "NovaOpenTelemetryRuntime");
        }

        private void DisposeSafely(
            IDisposable disposable,
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
                ReportSafely(
                    componentName +
                    " kapatılırken hata oluştu.",
                    exception);
            }
        }

        private void ReportSafely(
            string message,
            Exception exception)
        {
            if (_diagnosticHandler == null)
                return;

            try
            {
                _diagnosticHandler(
                    message,
                    exception);
            }
            catch
            {
                /*
                 * Diagnostic callback business
                 * uygulamasına hata taşıyamaz.
                 */
            }
        }

        private void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(
                    "NovaLegacyTelemetryRuntime");
            }
        }
    }
}
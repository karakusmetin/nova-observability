using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nova.Observability.Hosting;
using Nova.Observability.Sample.Worker.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nova.Observability.Sample.Worker;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;


    private readonly ISampleMessageProcessor
    _messageProcessor;

    private readonly NovaObservabilityState
        _observabilityState;

    public Worker(
        ILogger<Worker> logger,
        ISampleMessageProcessor messageProcessor,
        NovaObservabilityState observabilityState)
    {
        _logger =
            logger;

        _messageProcessor =
            messageProcessor;

        _observabilityState =
            observabilityState;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Sample Worker başladı. NovaEnabled={NovaEnabled}, DisabledReason={DisabledReason}",
            _observabilityState.IsEnabled,
            _observabilityState.DisabledReason);

        long iteration = 0;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                iteration++;

                var messageId =
                    100_000 + iteration;

                /*
                 * Her dördüncü işlem bilinçli olarak
                 * hata üretecek.
                 *
                 * Random kullanmıyoruz.
                 * Test/demomuz deterministik olsun.
                 */
                var simulateFailure =
                    iteration % 4 == 0;

                try
                {
                    await _messageProcessor
                        .ProcessAsync(
                            messageId,
                            simulateFailure,
                            stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    /*
                     * Processor business hatasını
                     * telemetry'ye kaydetti ve tekrar fırlattı.
                     *
                     * Worker'ın politikası ise:
                     * bu örnekte bir kayıt hatası yüzünden
                     * servisi durdurmamak.
                     */
                    _logger.LogWarning(
                        exception,
                        "Mesaj işlenemedi ancak Sample Worker çalışmaya devam edecek. MessageId={MessageId}",
                        messageId);
                }

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(5),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        finally
        {
            _logger.LogInformation(
                "Sample Worker durduruluyor.");
        }
    }
}
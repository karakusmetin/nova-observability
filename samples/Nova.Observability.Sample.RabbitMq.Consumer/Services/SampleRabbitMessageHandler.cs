using Nova.Observability.Abstractions;
using Nova.Observability.Core;

namespace Nova.Observability.Sample.RabbitMq.Consumer.Services;

public sealed class SampleRabbitMessageHandler :
    ISampleRabbitMessageHandler
{
    private readonly ILogger<
        SampleRabbitMessageHandler> _logger;

    public SampleRabbitMessageHandler(
        ILogger<SampleRabbitMessageHandler> logger)
    {
        _logger = logger;
    }

    [ObserveOperation(
        "sample.rabbitmq.message.handle",
        DisplayName =
            "RabbitMQ sample mesaj işleme",
        Kind =
            OperationKind.Internal,
        Domain =
            "Sample",
        Action =
            "Process",
        EntityType =
            "RabbitMessage",
        EntityIdParameter =
            "messageId")]
    public async Task HandleAsync(
        string messageId,
        bool simulateFailure,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Business mesaj işleme başladı. MessageId={MessageId}",
            messageId);

        NovaTelemetry.AddStep(
            "message.validated",
            "Mesaj doğrulandı.");

        await Task.Delay(
            350,
            cancellationToken);

        if (simulateFailure)
        {
            throw new InvalidOperationException(
                "Sample RabbitMQ business hatası.");
        }

        NovaTelemetry.AddStep(
            "business.completed",
            "Business işlem başarıyla tamamlandı.");

        _logger.LogInformation(
            "Business mesaj başarıyla işlendi. MessageId={MessageId}",
            messageId);
    }
}
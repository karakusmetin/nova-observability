namespace Nova.Observability.Sample.RabbitMq.Consumer.Services;

public interface ISampleRabbitMessageHandler
{
    Task HandleAsync(
        string messageId,
        bool simulateFailure,
        CancellationToken cancellationToken);
}
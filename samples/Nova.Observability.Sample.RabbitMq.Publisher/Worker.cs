using System.Text;
using Nova.Observability.Messaging;
using RabbitMQ.Client;

namespace Nova.Observability.Sample.RabbitMq.Publisher;

public sealed class Worker : BackgroundService
{
    private const string QueueName =
        "nova.observability.sample.rabbitmq";

    private readonly ILogger<Worker> _logger;

    public Worker(
        ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var factory =
            new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest",

                AutomaticRecoveryEnabled =
                    true,

                ClientProvidedName =
                    "nova-observability-sample-publisher"
            };

        await using var connection =
            await factory
                .CreateConnectionAsync();

        await using var channel =
            await connection
                .CreateChannelAsync();

        await channel.QueueDeclareAsync(
            QueueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        long counter = 0;

        while (!stoppingToken
               .IsCancellationRequested)
        {
            counter++;

            var messageId =
                $"sample-{counter:000000}";

            var simulateFailure =
                counter % 4 == 0;

            var payload =
                $"{messageId}|{simulateFailure}";

            var body =
                Encoding.UTF8.GetBytes(
                    payload);

            var properties =
                new BasicProperties
                {
                    MessageId =
                        messageId,

                    ContentType =
                        "text/plain",

                    Headers =
                        new Dictionary<
                            string,
                            object?>()
                };

            using var operation =
                NovaMessagingTelemetry
                    .StartProducerOperation(
                        properties.Headers,
                        messagingSystem:
                            "rabbitmq",
                        destinationName:
                            QueueName,
                        messageId:
                            messageId,
                        systemOperationName:
                            "publish");

            try
            {
                _logger.LogInformation(
                    "RabbitMQ mesajı gönderiliyor. MessageId={MessageId}, SimulateFailure={SimulateFailure}",
                    messageId,
                    simulateFailure);

                await channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: QueueName,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation(
                    "RabbitMQ mesajı gönderildi. MessageId={MessageId}",
                    messageId);

                operation.Complete();
            }
            catch (Exception exception)
            {
                /*
                 * Activity kapanmadan önce log.
                 */
                _logger.LogError(
                    exception,
                    "RabbitMQ mesajı gönderilemedi. MessageId={MessageId}",
                    messageId);

                operation.Fail(
                    exception);

                /*
                 * Sample publisher'ın tamamen
                 * kapanmasını istemiyoruz.
                 */
            }

            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken);
        }
    }
}
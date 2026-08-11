using System.Text;
using Nova.Observability.Messaging;
using Nova.Observability.Sample.RabbitMq.Consumer.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Nova.Observability.Sample.RabbitMq.Consumer;

public sealed class Worker :
    BackgroundService
{
    private const string QueueName =
        "nova.observability.sample.rabbitmq";

    private readonly ILogger<Worker>
        _logger;

    private readonly ISampleRabbitMessageHandler
        _handler;

    public Worker(
        ILogger<Worker> logger,
        ISampleRabbitMessageHandler handler)
    {
        _logger = logger;
        _handler = handler;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var factory =
            new ConnectionFactory
            {
                HostName =
                    "localhost",

                UserName =
                    "guest",

                Password =
                    "guest",

                AutomaticRecoveryEnabled =
                    true,

                ClientProvidedName =
                    "nova-observability-sample-consumer"
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

        var consumer =
            new AsyncEventingBasicConsumer(
                channel);

        consumer.ReceivedAsync +=
            async (_, args) =>
            {
                /*
                 * RabbitMQ 7.x message body'si
                 * callback sonrasında tutulmamalı.
                 * Önce kopyalıyoruz.
                 */
                var bodyBytes =
                    args.Body.ToArray();

                var payload =
                    Encoding.UTF8.GetString(
                        bodyBytes);

                var parts =
                    payload.Split('|');

                var messageId =
                    args.BasicProperties.MessageId
                    ??
                    parts[0];

                var simulateFailure =
                    parts.Length > 1 &&
                    bool.TryParse(
                        parts[1],
                        out var parsed) &&
                    parsed;

                IDictionary<string, object?>?
                    headers =
                        args.BasicProperties.Headers;

                using var operation =
                    NovaMessagingTelemetry
                        .StartConsumerOperation(
                            headers,
                            messagingSystem:
                                "rabbitmq",
                            destinationName:
                                QueueName,
                            messageId:
                                messageId,
                            systemOperationName:
                                "process");

                try
                {
                    _logger.LogInformation(
                        "RabbitMQ mesajı alındı. MessageId={MessageId}",
                        messageId);

                    await _handler.HandleAsync(
                        messageId,
                        simulateFailure,
                        stoppingToken);

                    _logger.LogInformation(
                        "RabbitMQ mesajı işlendi. MessageId={MessageId}",
                        messageId);

                    operation.Complete();

                    await channel.BasicAckAsync(
                        args.DeliveryTag,
                        multiple: false);
                }
                catch (Exception exception)
                {
                    /*
                     * Error log aktif consumer
                     * Activity kapanmadan yazılmalı.
                     */
                    _logger.LogError(
                        exception,
                        "RabbitMQ mesajı işlenemedi. MessageId={MessageId}",
                        messageId);

                    operation.Fail(
                        exception);

                    await channel.BasicNackAsync(
                        args.DeliveryTag,
                        multiple: false,
                        requeue: false);
                }
            };

        await channel.BasicConsumeAsync(
            QueueName,
            autoAck: false,
            consumer);

        _logger.LogInformation(
            "RabbitMQ consumer hazır. Queue={Queue}",
            QueueName);

        try
        {
            await Task.Delay(
                Timeout.Infinite,
                stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken
                .IsCancellationRequested)
        {
        }
    }
}
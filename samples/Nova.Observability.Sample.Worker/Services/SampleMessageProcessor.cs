using Microsoft.Extensions.Logging;
using Nova.Observability.Abstractions;
using Nova.Observability.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nova.Observability.Sample.Worker.Services;

public sealed class SampleMessageProcessor : ISampleMessageProcessor
{
    private readonly ILogger<SampleMessageProcessor>
        _logger;

    public SampleMessageProcessor(
        ILogger<SampleMessageProcessor> logger)
    {
        _logger =
            logger;
    }

    [ObserveOperation(
        "sample.message.process",
        DisplayName = "Sample mesaj işleme",
        Kind = OperationKind.Consumer,
        Domain = "Sample",
        Action = "Process",
        EntityType = "SampleMessage",
        EntityIdParameter = "messageId")]
    public async Task ProcessAsync(
        long messageId,
        bool simulateFailure,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Mesaj işleme başladı. MessageId={MessageId}",
            messageId);

        NovaTelemetry.AddStep(
            "message.received",
            "Mesaj Worker tarafından işleme alındı.");

        await Task.Delay(
            250,
            cancellationToken);

        NovaTelemetry.AddStep(
            "message.validated",
            "Mesaj doğrulandı.");

        _logger.LogInformation(
            "Mesaj doğrulandı. MessageId={MessageId}",
            messageId);

        await PersistDocumentAsync(
            messageId,
            cancellationToken);

        if (simulateFailure)
        {
            throw new InvalidOperationException(
                "Sample amaçlı oluşturulan business işlem hatası.");
        }

        NovaTelemetry.AddStep(
            "message.completed",
            "Mesaj başarıyla işlendi.");

        _logger.LogInformation(
            "Mesaj başarıyla işlendi. MessageId={MessageId}",
            messageId);
    }

    private async Task PersistDocumentAsync(
        long messageId,
        CancellationToken cancellationToken)
    {
        using var operation =
            NovaTelemetry.StartOperation(
                "sample.document.persist",
                new NovaOperationOptions
                {
                    DisplayName =
                        "Sample doküman kaydetme",

                    Kind =
                        OperationKind.Internal,

                    Domain =
                        "Sample",

                    Action =
                        "Persist",

                    EntityType =
                        "SampleMessage",

                    EntityId =
                        messageId.ToString()
                });

        try
        {
            operation.Step(
                "persistence.started",
                "Doküman kaydetme işlemi başladı.");

            await Task.Delay(
                350,
                cancellationToken);

            operation.SetTag(
                "sample.storage.type",
                "memory");

            operation.Step(
                "persistence.completed",
                "Doküman başarıyla kaydedildi.");

            operation.Complete();
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            operation.Cancel(
                "Worker cancellation requested.");

            throw;
        }
        catch (Exception exception)
        {
            operation.Fail(
                exception);

            throw;
        }
    }
}
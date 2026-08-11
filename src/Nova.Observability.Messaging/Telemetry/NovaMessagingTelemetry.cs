using System;
using System.Collections.Generic;
using Nova.Observability.Abstractions;
using Nova.Observability.Core;

namespace Nova.Observability.Messaging;

public static class NovaMessagingTelemetry
{
    public static INovaOperation StartProducerOperation(
        IDictionary<string, object?> headers,
        string messagingSystem,
        string destinationName,
        string? messageId = null,
        string systemOperationName = "send",
        Action<string, Exception?>? diagnosticHandler = null)
    {
        ValidateRequired(
            messagingSystem,
            nameof(messagingSystem));

        ValidateRequired(
            destinationName,
            nameof(destinationName));

        var operation =
            NovaTelemetry.StartOperation(
                "messaging.send",
                CreateOptions(
                    OperationKind.Producer,
                    messagingSystem,
                    destinationName,
                    messageId,
                    systemOperationName,
                    "send"));

        /*
         * Operation başladıktan sonra Activity.Current
         * producer Activity'dir.
         *
         * Header'a tam olarak onun context'ini yazıyoruz.
         */
        NovaTraceContextPropagation
            .TryInjectCurrentContext(
                headers,
                diagnosticHandler);

        return operation;
    }

    public static INovaOperation StartConsumerOperation(
        IDictionary<string, object?>? headers,
        string messagingSystem,
        string destinationName,
        string? messageId = null,
        string systemOperationName = "process",
        Action<string, Exception?>? diagnosticHandler = null)
    {
        ValidateRequired(
            messagingSystem,
            nameof(messagingSystem));

        ValidateRequired(
            destinationName,
            nameof(destinationName));

        var options =
            CreateOptions(
                OperationKind.Consumer,
                messagingSystem,
                destinationName,
                messageId,
                systemOperationName,
                "process");

        if (NovaTraceContextPropagation
            .TryExtractParentContext(
                headers,
                out var parentContext,
                diagnosticHandler))
        {
            return NovaTelemetry.StartOperation(
                "messaging.process",
                parentContext,
                options);
        }

        /*
         * Header yok veya bozuk.
         *
         * Mesaj yine işlenecek.
         * Sadece yeni bağımsız trace oluşur.
         */
        return NovaTelemetry.StartOperation(
            "messaging.process",
            options);
    }

    private static NovaOperationOptions CreateOptions(
        OperationKind kind,
        string messagingSystem,
        string destinationName,
        string? messageId,
        string systemOperationName,
        string operationType)
    {
        var tags =
            new List<KeyValuePair<string, object?>>
            {
                new(
                    NovaMessagingTags.MessagingSystem,
                    messagingSystem),

                new(
                    NovaMessagingTags.DestinationName,
                    destinationName),

                new(
                    NovaMessagingTags.OperationName,
                    systemOperationName),

                new(
                    NovaMessagingTags.OperationType,
                    operationType)
            };

        if (!string.IsNullOrWhiteSpace(
                messageId))
        {
            tags.Add(
                new KeyValuePair<string, object?>(
                    NovaMessagingTags.MessageId,
                    messageId));
        }

        return new NovaOperationOptions
        {
            DisplayName =
                systemOperationName +
                " " +
                destinationName,

            Kind =
                kind,

            Domain =
                "Messaging",

            Action =
                operationType,

            EntityType =
                "Message",

            EntityId =
                messageId,

            Tags =
                tags
        };
    }

    private static void ValidateRequired(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            throw new ArgumentException(
                parameterName +
                " boş olamaz.",
                parameterName);
        }
    }
}
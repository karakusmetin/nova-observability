using System;
using Microsoft.Extensions.Logging;
using Nova.Observability.Abstractions;
using Nova.Observability.Core;

namespace Nova.Observability.Sample.LegacyConsole
{
    public sealed class LegacyDocumentService :
        ILegacyDocumentService
    {
        private readonly ILogger<LegacyDocumentService>
            _logger;

        public LegacyDocumentService(
            ILogger<LegacyDocumentService> logger)
        {
            _logger = logger;
        }

        [ObserveOperation(
            "legacy.document.process",
            DisplayName =
                "Legacy doküman işleme",
            Kind =
                OperationKind.Internal,
            Domain =
                "Document",
            Action =
                "Process",
            EntityType =
                "Document",
            EntityIdParameter =
                "documentId")]
        public void Process(
            long documentId,
            bool simulateFailure)
        {
            _logger.LogInformation(
                "Legacy doküman işleme başladı. DocumentId={DocumentId}",
                documentId);

            NovaTelemetry.AddStep(
                "document.validated",
                "Doküman doğrulandı.");

            if (simulateFailure)
            {
                throw new InvalidOperationException(
                    "Sample legacy business hatası.");
            }

            NovaTelemetry.AddStep(
                "document.completed",
                "Doküman başarıyla işlendi.");

            _logger.LogInformation(
                "Legacy doküman başarıyla işlendi. DocumentId={DocumentId}",
                documentId);
        }
    }
}
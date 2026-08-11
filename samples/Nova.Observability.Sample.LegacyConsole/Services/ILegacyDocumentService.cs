namespace Nova.Observability.Sample.LegacyConsole
{
    public interface ILegacyDocumentService
    {
        void Process(
            long documentId,
            bool simulateFailure);
    }
}
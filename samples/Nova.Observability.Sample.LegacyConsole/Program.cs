using System;
using Microsoft.Extensions.Logging;
using Nova.Observability.Legacy;

namespace Nova.Observability.Sample.LegacyConsole
{
    internal static class Program
    {
        private static void Main(
            string[] args)
        {
            using (
                var telemetry =
                    NovaLegacyTelemetry
                        .StartFromAppConfig())
            {
                Console.WriteLine(
                    "Nova Legacy Enabled: " +
                    telemetry.IsEnabled);

                var logger =
                    telemetry
                        .CreateLogger<
                            LegacyDocumentService>();

                ILegacyDocumentService service =
                    new LegacyDocumentService(
                        logger);

                service =
                    telemetry
                        .CreateObserved<
                            ILegacyDocumentService>(
                            service);

                for (var index = 1;
                     index <= 5;
                     index++)
                {
                    var documentId =
                        200000 + index;

                    var simulateFailure =
                        index == 4;

                    try
                    {
                        service.Process(
                            documentId,
                            simulateFailure);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(
                            "Business hata yakalandı: " +
                            exception.Message);
                    }
                }

                telemetry.TryForceFlush();

                Console.WriteLine(
                    "Legacy sample tamamlandı.");
            }

            Console.WriteLine(
                "Çıkmak için bir tuşa basın.");

            Console.ReadKey();
        }
    }
}
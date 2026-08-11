using System.Threading;
using System.Threading.Tasks;

namespace Nova.Observability.Sample.Worker.Services;

public interface ISampleMessageProcessor
{
    Task ProcessAsync(
        long messageId,
        bool simulateFailure,
        CancellationToken cancellationToken);
}
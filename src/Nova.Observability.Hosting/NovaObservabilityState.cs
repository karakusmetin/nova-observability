namespace Nova.Observability.Hosting;

public sealed class NovaObservabilityState
{
    internal NovaObservabilityState(
        bool isEnabled,
        string? disabledReason)
    {
        IsEnabled = isEnabled;
        DisabledReason = disabledReason;
    }

    public bool IsEnabled { get; }

    public string? DisabledReason { get; }
}
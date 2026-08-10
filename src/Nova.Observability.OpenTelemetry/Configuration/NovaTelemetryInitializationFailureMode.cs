namespace Nova.Observability.OpenTelemetry;

public enum NovaTelemetryInitializationFailureMode
{
    /// <summary>
    /// Telemetry başlatılamazsa uygulama telemetry olmadan çalışmaya devam eder.
    /// Üretim ortamı için varsayılan davranıştır.
    /// </summary>
    ContinueWithoutTelemetry = 0,

    /// <summary>
    /// Telemetry başlatılamazsa başlangıç exception'ı uygulamaya iletilir.
    /// Test ve doğrulama ortamlarında kullanılabilir.
    /// </summary>
    Throw = 1
}
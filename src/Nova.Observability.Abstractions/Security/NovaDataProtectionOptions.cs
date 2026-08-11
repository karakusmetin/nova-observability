using System.Collections.Generic;

namespace Nova.Observability.Abstractions;

public sealed class NovaDataProtectionOptions
{
    public bool Enabled { get; set; } =
        true;

    public string RedactedValue { get; set; } =
        "[REDACTED]";

    public int MaxAttributeValueLength { get; set; } =
        2048;

    public int MaxLogMessageLength { get; set; } =
        8192;

    public int MaxExceptionMessageLength { get; set; } =
        4096;

    public int MaxExceptionStackTraceLength { get; set; } =
        16384;

    public bool ReplaceBinaryValues { get; set; } =
        true;

    public bool ReplaceComplexValues { get; set; } =
        true;

    public IList<string> SensitiveKeyFragments { get; } =
        new List<string>
        {
            "password",
            "passwd",
            "clientsecret",
            "apikey",
            "accesstoken",
            "refreshtoken",
            "authorization",
            "connectionstring",
            "privatekey",
            "credential",
            "setcookie",
            "sessionid"
        };

    /// <summary>
    /// Uygulamaya özel regex pattern'leri.
    /// Eşleşen değerler tamamen REDACTED yapılır.
    /// </summary>
    public IList<string> AdditionalRedactionPatterns { get; } =
        new List<string>();
}
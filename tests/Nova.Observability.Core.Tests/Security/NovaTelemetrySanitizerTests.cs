using Nova.Observability.Abstractions;
using Nova.Observability.Core;
using Xunit;

namespace Nova.Observability.Core.Tests.Security;

public sealed class NovaTelemetrySanitizerTests
{
    [Fact]
    public void SensitiveKey_ShouldBeRedacted()
    {
        var sanitizer =
            CreateSanitizer();

        var result =
            sanitizer.ProtectAttribute(
                "Password",
                "super-secret");

        Assert.Equal(
            "[REDACTED]",
            result);
    }

    [Fact]
    public void ApiKey_ShouldBeRedacted()
    {
        var sanitizer =
            CreateSanitizer();

        var result =
            sanitizer.ProtectAttribute(
                "api_key",
                "abc-123");

        Assert.Equal(
            "[REDACTED]",
            result);
    }

    [Fact]
    public void NormalValue_ShouldRemainVisible()
    {
        var sanitizer =
            CreateSanitizer();

        var result =
            sanitizer.ProtectAttribute(
                "DocumentId",
                200001L);

        Assert.Equal(
            200001L,
            result);
    }

    [Fact]
    public void CredentialInsideText_ShouldBeRedacted()
    {
        var sanitizer =
            CreateSanitizer();

        var result =
            sanitizer.ProtectLogMessage(
                "Login failed. password=my-secret");

        Assert.Equal(
            "Login failed. password=[REDACTED]",
            result);
    }

    [Fact]
    public void LongText_ShouldBeTruncated()
    {
        var options =
            new NovaDataProtectionOptions
            {
                MaxAttributeValueLength = 10
            };

        var sanitizer =
            new NovaTelemetrySanitizer(
                options);

        var result =
            sanitizer.ProtectAttribute(
                "Message",
                "123456789012345");

        Assert.Equal(
            "1234567890...[TRUNCATED]",
            result);
    }

    [Fact]
    public void ComplexObject_ShouldNotBeSerialized()
    {
        var sanitizer =
            CreateSanitizer();

        var result =
            sanitizer.ProtectAttribute(
                "Request",
                new TestRequest());

        Assert.Equal(
            "<complex:TestRequest>",
            result);
    }

    private static NovaTelemetrySanitizer
        CreateSanitizer()
    {
        return new NovaTelemetrySanitizer(
            new NovaDataProtectionOptions());
    }

    private sealed class TestRequest
    {
        public string Password { get; set; } =
            "should-never-leak";
    }
}
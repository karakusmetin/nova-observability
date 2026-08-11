using System.Collections.Generic;
using System.Diagnostics;
using Nova.Observability.Messaging;
using Xunit;

namespace Nova.Observability.Core.Tests.Messaging;

public sealed class NovaTraceContextPropagationTests
{
    [Fact]
    public void InjectAndExtract_ShouldPreserveTraceId()
    {
        var headers =
            new Dictionary<string, object?>();

        using var activity =
            new Activity(
                "producer")
                .SetIdFormat(
                    ActivityIdFormat.W3C)
                .Start();

        Assert.NotNull(
            activity);

        var injected =
            NovaTraceContextPropagation
                .TryInjectCurrentContext(
                    headers);

        var extracted =
            NovaTraceContextPropagation
                .TryExtractParentContext(
                    headers,
                    out var parentContext);

        Assert.True(
            injected);

        Assert.True(
            extracted);

        Assert.Equal(
            activity.TraceId,
            parentContext.TraceId);
    }

    [Fact]
    public void MissingHeaders_ShouldFailOpen()
    {
        var result =
            NovaTraceContextPropagation
                .TryExtractParentContext(
                    null,
                    out var parentContext);

        Assert.False(
            result);

        Assert.Equal(
            default,
            parentContext);
    }

    [Fact]
    public void InvalidTraceParent_ShouldFailOpen()
    {
        var headers =
            new Dictionary<string, object?>
            {
                ["traceparent"] =
                    new byte[]
                    {
                        1,
                        2,
                        3
                    }
            };

        var result =
            NovaTraceContextPropagation
                .TryExtractParentContext(
                    headers,
                    out _);

        Assert.False(
            result);
    }
}
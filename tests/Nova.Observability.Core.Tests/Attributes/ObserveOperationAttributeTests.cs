using Nova.Observability.Abstractions;
using Xunit;

namespace Nova.Observability.Core.Tests.Attributes;

public sealed class ObserveOperationAttributeTests
{
    [Fact]
    public void Constructor_ShouldSetOperationName()
    {
        var attribute =
            new ObserveOperationAttribute(
                "sample.message.process");

        Assert.Equal(
            "sample.message.process",
            attribute.Name);
    }

    [Fact]
    public void Defaults_ShouldBeSafe()
    {
        var attribute =
            new ObserveOperationAttribute(
                "sample.operation");

        Assert.Equal(
            OperationKind.Internal,
            attribute.Kind);

        Assert.Null(
            attribute.DisplayName);

        Assert.Null(
            attribute.Domain);

        Assert.Null(
            attribute.Action);

        Assert.Null(
            attribute.EntityType);

        Assert.Null(
            attribute.EntityIdParameter);

        Assert.Null(
            attribute.CorrelationIdParameter);
    }

    [Fact]
    public void Properties_ShouldStoreOperationMetadata()
    {
        var attribute =
            new ObserveOperationAttribute(
                "kep.detail.process")
            {
                DisplayName =
                    "KEP gelen detay işleme",

                Kind =
                    OperationKind.Consumer,

                Domain =
                    "KEP",

                Action =
                    "Process",

                EntityType =
                    "KEPGelen",

                EntityIdParameter =
                    "kepGelenId",

                CorrelationIdParameter =
                    "correlationId"
            };

        Assert.Equal(
            "kep.detail.process",
            attribute.Name);

        Assert.Equal(
            "KEP gelen detay işleme",
            attribute.DisplayName);

        Assert.Equal(
            OperationKind.Consumer,
            attribute.Kind);

        Assert.Equal(
            "KEP",
            attribute.Domain);

        Assert.Equal(
            "Process",
            attribute.Action);

        Assert.Equal(
            "KEPGelen",
            attribute.EntityType);

        Assert.Equal(
            "kepGelenId",
            attribute.EntityIdParameter);

        Assert.Equal(
            "correlationId",
            attribute.CorrelationIdParameter);
    }
}
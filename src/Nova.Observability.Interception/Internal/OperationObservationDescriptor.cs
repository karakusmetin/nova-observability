using System;
using System.Globalization;
using Nova.Observability.Abstractions;

namespace Nova.Observability.Interception;

internal sealed class OperationObservationDescriptor
{
    internal OperationObservationDescriptor(
        ObserveOperationAttribute attribute,
        int entityIdParameterIndex,
        int correlationIdParameterIndex)
    {
        Attribute =
            attribute;

        EntityIdParameterIndex =
            entityIdParameterIndex;

        CorrelationIdParameterIndex =
            correlationIdParameterIndex;
    }

    internal ObserveOperationAttribute Attribute
    {
        get;
    }

    internal int EntityIdParameterIndex
    {
        get;
    }

    internal int CorrelationIdParameterIndex
    {
        get;
    }

    internal NovaOperationOptions CreateOptions(
        object?[] arguments)
    {
        return new NovaOperationOptions
        {
            DisplayName =
                Attribute.DisplayName,

            Kind =
                Attribute.Kind,

            Domain =
                Attribute.Domain,

            Action =
                Attribute.Action,

            EntityType =
                Attribute.EntityType,

            EntityId =
                GetArgumentAsString(
                    arguments,
                    EntityIdParameterIndex),

            CorrelationId =
                GetArgumentAsString(
                    arguments,
                    CorrelationIdParameterIndex)
        };
    }

    private static string? GetArgumentAsString(
        object?[] arguments,
        int index)
    {
        if (index < 0 ||
            index >= arguments.Length)
        {
            return null;
        }

        var value =
            arguments[index];

        if (value == null)
            return null;

        if (value is string text)
            return text;

        if (value is IFormattable formattable)
        {
            return formattable.ToString(
                null,
                CultureInfo.InvariantCulture);
        }

        return value.ToString();
    }
}
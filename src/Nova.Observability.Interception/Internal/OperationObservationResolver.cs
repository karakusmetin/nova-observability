using System;
using System.Reflection;
using Castle.DynamicProxy;
using Nova.Observability.Abstractions;

namespace Nova.Observability.Interception;

internal static class OperationObservationResolver
{
    internal static bool TryResolve(
        IInvocation invocation,
        out OperationObservationDescriptor? descriptor,
        out string? errorMessage)
    {
        descriptor =
            null;

        errorMessage =
            null;

        var targetMethod =
            TryGetTargetMethod(invocation);

        var attribute =
            targetMethod?
                .GetCustomAttribute<
                    ObserveOperationAttribute>(
                    inherit: true);

        var metadataMethod =
            targetMethod;

        if (attribute == null)
        {
            attribute =
                invocation.Method
                    .GetCustomAttribute<
                        ObserveOperationAttribute>(
                        inherit: true);

            metadataMethod =
                invocation.Method;
        }

        if (attribute == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(
                attribute.Name))
        {
            errorMessage =
                "ObserveOperation Name boş olamaz.";

            return false;
        }

        if (metadataMethod == null)
        {
            errorMessage =
                "ObserveOperation için method metadata çözümlenemedi.";

            return false;
        }

        var entityIndex =
            ResolveParameterIndex(
                metadataMethod,
                attribute.EntityIdParameter,
                out var entityError);

        if (entityError != null)
        {
            errorMessage =
                entityError;

            return false;
        }

        var correlationIndex =
            ResolveParameterIndex(
                metadataMethod,
                attribute.CorrelationIdParameter,
                out var correlationError);

        if (correlationError != null)
        {
            errorMessage =
                correlationError;

            return false;
        }

        descriptor =
            new OperationObservationDescriptor(
                attribute,
                entityIndex,
                correlationIndex);

        return true;
    }

    private static MethodInfo? TryGetTargetMethod(
        IInvocation invocation)
    {
        try
        {
            return invocation.MethodInvocationTarget;
        }
        catch
        {
            return null;
        }
    }

    private static int ResolveParameterIndex(
        MethodInfo method,
        string? parameterName,
        out string? errorMessage)
    {
        errorMessage =
            null;

        if (string.IsNullOrWhiteSpace(
                parameterName))
        {
            return -1;
        }

        var parameters =
            method.GetParameters();

        for (var index = 0;
             index < parameters.Length;
             index++)
        {
            if (string.Equals(
                    parameters[index].Name,
                    parameterName,
                    StringComparison.Ordinal))
            {
                return index;
            }
        }

        errorMessage =
            $"'{parameterName}' isimli parameter " +
            $"'{method.DeclaringType?.FullName}.{method.Name}' " +
            "metodunda bulunamadı.";

        return -1;
    }
}
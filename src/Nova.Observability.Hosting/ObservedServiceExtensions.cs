using System;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nova.Observability.Interception;

namespace Nova.Observability.Hosting;

public static class ObservedServiceExtensions
{
    public static IServiceCollection
        AddNovaObservedSingleton<
            TService,
            TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation :
            class,
            TService
    {
        EnsureInterface<TService>();

        services
            .TryAddSingleton<TImplementation>();

        services.AddSingleton<TService>(
            serviceProvider =>
                CreateProxy<
                    TService,
                    TImplementation>(
                    serviceProvider));

        return services;
    }

    public static IServiceCollection
        AddNovaObservedScoped<
            TService,
            TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation :
            class,
            TService
    {
        EnsureInterface<TService>();

        services
            .TryAddScoped<TImplementation>();

        services.AddScoped<TService>(
            serviceProvider =>
                CreateProxy<
                    TService,
                    TImplementation>(
                    serviceProvider));

        return services;
    }

    public static IServiceCollection
        AddNovaObservedTransient<
            TService,
            TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation :
            class,
            TService
    {
        EnsureInterface<TService>();

        services
            .TryAddTransient<TImplementation>();

        services.AddTransient<TService>(
            serviceProvider =>
                CreateProxy<
                    TService,
                    TImplementation>(
                    serviceProvider));

        return services;
    }

    private static TService CreateProxy<
        TService,
        TImplementation>(
        IServiceProvider serviceProvider)
        where TService : class
        where TImplementation :
            class,
            TService
    {
        var target =
            serviceProvider
                .GetRequiredService<
                    TImplementation>();

        var proxyGenerator =
            serviceProvider
                .GetService<
                    IProxyGenerator>();

        var interceptor =
            serviceProvider
                .GetService<
                    NovaOperationInterceptor>();

        /*
         * Nova devre dışıysa veya interceptor
         * kurulamamışsa gerçek service'i döndür.
         *
         * Business uygulaması çalışmaya devam eder.
         */
        if (proxyGenerator == null ||
            interceptor == null)
        {
            return target;
        }

        try
        {
            return proxyGenerator
                .CreateInterfaceProxyWithTarget<
                    TService>(
                    target,
                    interceptor);
        }
        catch
        {
            /*
             * Proxy oluşturma sorunu business
             * service registration'ını bozmamalı.
             */
            return target;
        }
    }

    private static void EnsureInterface<TService>()
    {
        if (!typeof(TService).IsInterface)
        {
            throw new InvalidOperationException(
                $"{typeof(TService).FullName} bir interface olmalıdır. " +
                "İlk Nova interception sürümü interface proxy kullanır.");
        }
    }
}
using Castle.DynamicProxy;
using Nova.Observability.Abstractions;
using Nova.Observability.Interception;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Nova.Observability.Core.Tests.Interception;

public sealed class NovaOperationInterceptorTests
{
    [Fact]
    public void SyncMethod_ShouldReturnOriginalResult()
    {
        var service =
            CreateProxy<ISyncTestService>(
                new SyncTestService());

        var result =
            service.Calculate(10);

        Assert.Equal(
            20,
            result);
    }

    [Fact]
    public async Task TaskMethod_ShouldCompleteNormally()
    {
        var service =
            CreateProxy<IAsyncTestService>(
                new AsyncTestService());

        await service.ExecuteAsync(
            1001);

        Assert.True(
            service.WasExecuted);
    }

    [Fact]
    public async Task TaskFailure_ShouldPreserveOriginalException()
    {
        var service =
            CreateProxy<IFailingTestService>(
                new FailingTestService());

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                    () =>
                        service.ExecuteAsync());

        Assert.Equal(
            "Business failure.",
            exception.Message);
    }

    [Fact]
    public async Task InvalidEntityParameter_ShouldFailOpen()
    {
        var diagnostics =
            new List<string>();

        var options =
            new NovaInterceptionOptions
            {
                DiagnosticHandler =
                    (message, _) =>
                        diagnostics.Add(
                            message)
            };

        var service =
            CreateProxy<IInvalidMetadataService>(
                new InvalidMetadataService(),
                options);

        await service.ExecuteAsync(
            42);

        Assert.True(
            service.WasExecuted);

        Assert.NotEmpty(
            diagnostics);
    }

    [Fact]
    public async Task TaskOfT_ShouldReturnOriginalResult()
    {
        var service =
            CreateProxy<IResultTestService>(
                new ResultTestService());

        var result =
            await service.CalculateAsync(
                21);

        Assert.Equal(
            42,
            result);
    }

    public interface IResultTestService
    {
        Task<int> CalculateAsync(
            int value);
    }

    public sealed class ResultTestService :
        IResultTestService
    {
        [ObserveOperation(
            "test.async-result")]
        public async Task<int> CalculateAsync(
            int value)
        {
            await Task.Yield();

            return value * 2;
        }
    }

    private static TService CreateProxy<TService>(
        TService implementation,
        NovaInterceptionOptions? options = null)
        where TService : class
    {
        var proxyGenerator =
            new ProxyGenerator();

        var interceptor =
            new NovaOperationInterceptor(
                options ??
                new NovaInterceptionOptions());

        return proxyGenerator
            .CreateInterfaceProxyWithTarget(
                implementation,
                interceptor);
    }

    public interface ISyncTestService
    {
        int Calculate(
            int value);
    }

    public sealed class SyncTestService :
        ISyncTestService
    {
        [ObserveOperation(
            "test.sync")]
        public int Calculate(
            int value)
        {
            return value * 2;
        }
    }

    public interface IAsyncTestService
    {
        bool WasExecuted { get; }

        Task ExecuteAsync(
            long id);
    }

    public sealed class AsyncTestService :
     IAsyncTestService
    {
        public bool WasExecuted
        {
            get;
            private set;
        }

        [ObserveOperation(
            "test.async",
            EntityType = "TestEntity",
            EntityIdParameter = "id")]
        public async Task ExecuteAsync(
            long id)
        {
            await Task.Yield();

            WasExecuted = true;
        }
    }

    public interface IFailingTestService
    {
        Task ExecuteAsync();
    }

    public sealed class FailingTestService :
    IFailingTestService
    {
        [ObserveOperation(
            "test.failure")]
        public async Task ExecuteAsync()
        {
            await Task.Yield();

            throw new InvalidOperationException(
                "Business failure.");
        }
    }

    public interface IInvalidMetadataService
    {
        bool WasExecuted { get; }

        Task ExecuteAsync(
            long id);
    }

    public sealed class InvalidMetadataService :
        IInvalidMetadataService
    {
        public bool WasExecuted
        {
            get;
            private set;
        }

        [ObserveOperation(
            "test.invalid-metadata",
            EntityIdParameter =
                "olmayanParametre")]
        public Task ExecuteAsync(
            long id)
        {
            WasExecuted = true;

            return Task.CompletedTask;
        }
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using CelestiaCS.Lib.Linq;
using CelestiaCS.Lib.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace CelestiaCS.Lib.DependencyInjection;

public interface IInitialize
{
    Task InitAsync();

    internal static IInitialize Empty { get; } = new EmptyInitialize();
    private sealed class EmptyInitialize : IInitialize
    {
        public Task InitAsync() => Task.CompletedTask;
    }
}

public static class InitializeExtensions
{
    public static IServiceCollection AddInitialize(this IServiceCollection services, Type serviceType)
    {
        return services.AddTransient(s => (s.GetRequiredService(serviceType) as IInitialize) ?? IInitialize.Empty);
    }

    public static IServiceCollection AddInitialize<T>(this IServiceCollection services)
    {
        return services.AddInitialize(typeof(T));
    }

    public static Task RunInitializeAsync(this IServiceProvider services)
    {
        var initializers = services.GetServices<IInitialize>().Except(IInitialize.Empty);
        return Parallel.ForEachAsync(initializers, (i, _) => i.InitAsync().AsValueTask());
    }
}

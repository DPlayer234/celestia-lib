using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace CelestiaCS.Lib.DependencyInjection;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// Resolves a service and stores it in <paramref name="field"/>, unless that field already holds an instance.
    /// </summary>
    /// <typeparam name="T"> The type of service to resolve. </typeparam>
    /// <param name="services"> The service provider. </param>
    /// <param name="field"> The field to store/load the service from. </param>
    /// <returns> The loaded service. </returns>
    public static T GetLazyService<T>(this IServiceProvider services, [NotNull] ref T? field) where T : notnull
    {
        return field ??= services.GetRequiredService<T>();
    }
}

using System;
using Microsoft.Extensions.DependencyInjection;

namespace CelestiaCS.Lib.DependencyInjection;

/// <summary>
/// Provides delayed resolution for a service.
/// </summary>
/// <typeparam name="T"> The service type to resolve. </typeparam>
public interface IDelay<T>
    where T : notnull
{
    /// <summary> The service instance, resolved when first accessed. </summary>
    T Value { get; }
}

public static class DelayServiceExtensions
{
    /// <summary>
    /// Registers services needed to use <see cref="IDelay{T}"/>.
    /// </summary>
    /// <param name="services"> The service collection. </param>
    /// <returns> The same service collection. </returns>
    public static IServiceCollection AddDelayed(this IServiceCollection services)
    {
        return services.AddTransient(typeof(IDelay<>), typeof(Delay<>));
    }

    private sealed class Delay<T>(IServiceProvider provider) : IDelay<T>
        where T : notnull
    {
        private readonly object _lock = new();
        private IServiceProvider? _provider = provider;
        private T? _value;

        public T Value => _provider == null ? _value! : LoadValue();

        private T LoadValue()
        {
            lock (_lock)
            {
                if (_provider != null)
                {
                    _value = _provider.GetRequiredService<T>();
                    _provider = null;
                }

                return _value!;
            }
        }
    }
}

// Assembly 'Microsoft.Extensions.ObjectPool.DependencyInjection'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.ObjectPool;

/// <summary>
/// Contains configuration for pools.
/// </summary>
public sealed class DependencyInjectionPoolOptions
{
    /// <summary>
    /// Gets or sets the maximal capacity of the pool.
    /// </summary>
    /// <value>
    /// The default is 1024.
    /// </value>
    public int Capacity { get; set; }

    public DependencyInjectionPoolOptions();
}

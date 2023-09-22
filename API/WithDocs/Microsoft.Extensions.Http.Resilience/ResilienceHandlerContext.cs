// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using Microsoft.Extensions.Http.Resilience.Internal;
using Polly.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// The context used when building a resilience pipeline HTTP handler.
/// </summary>
public sealed class ResilienceHandlerContext
{
    /// <summary>
    /// Gets the service provider.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the name of the builder being built.
    /// </summary>
    public string BuilderName { get; }

    /// <summary>
    /// Gets the instance name of resilience pipeline being built.
    /// </summary>
    public string InstanceName { get; }

    /// <summary>
    /// Enables dynamic reloading of the resilience pipeline whenever the <typeparamref name="TOptions" /> options are changed.
    /// </summary>
    /// <typeparam name="TOptions">The options type to listen to.</typeparam>
    /// <param name="name">The named options, if any.</param>
    /// <remarks>
    /// You can decide based on the <paramref name="name" /> to listen for changes in global options or named options.
    /// If <paramref name="name" /> is <see langword="null" /> then the global options are listened to.
    /// <para>
    /// You can listen for changes only for single options. If you call this method multiple times, the preceding calls are ignored and only the last one wins.
    /// </para>
    /// </remarks>
    public void EnableReloads<TOptions>(string? name = null);
}

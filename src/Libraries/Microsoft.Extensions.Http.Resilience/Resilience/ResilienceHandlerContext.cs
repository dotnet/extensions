// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Http.Resilience.Internal;
using Polly.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// The context used when building a resilience strategy HTTP handler.
/// </summary>
public class ResilienceHandlerContext
{
    private readonly AddResilienceStrategyContext<HttpKey> _context;

    internal ResilienceHandlerContext(AddResilienceStrategyContext<HttpKey> context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    public IServiceProvider ServiceProvider => _context.ServiceProvider;

    /// <summary>
    /// Gets the name of the builder being built.
    /// </summary>
    public string BuilderName => _context.StrategyKey.Name;

    /// <summary>
    /// Gets the strategy key of the resilience strategy being built.
    /// </summary>
    public string StrategyKey => _context.StrategyKey.Key;

    /// <summary>
    /// Enables dynamic reloading of the resilience strategy whenever the <typeparamref name="TOptions"/> options are changed.
    /// </summary>
    /// <typeparam name="TOptions">The options type to listen to.</typeparam>
    /// <param name="name">The named options, if any.</param>
    /// <remarks>
    /// You can decide based on the <paramref name="name"/> to listen for changes in global options or named options.
    /// If <paramref name="name"/> is <see langword="null"/> then the global options are listened to.
    /// <para>
    /// You can listen for changes only for single options. If you call this method multiple times, the preceding calls are ignored and only the last one wins.
    /// </para>
    /// </remarks>
    public void EnableReloads<TOptions>(string? name = null) => _context.EnableReloads<TOptions>(name);

    internal T GetOptions<T>(string name) => _context.GetOptions<T>(name);
}

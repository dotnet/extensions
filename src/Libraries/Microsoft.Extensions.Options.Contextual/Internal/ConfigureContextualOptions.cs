// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options.Contextual.Provider;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Options.Contextual.Internal;

/// <summary>
/// Configures the <typeparamref name="TOptions"/> type.
/// </summary>
/// <typeparam name="TOptions">The type of options configured.</typeparam>
internal sealed class ConfigureContextualOptions<TOptions> : IConfigureContextualOptions<TOptions>
    where TOptions : class
{
    private readonly IOptionsContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureContextualOptions{TOptions}"/> class.
    /// </summary>
    /// <param name="configureOptions">The action to apply to configure options.</param>
    /// <param name="context">The context used to configure the options.</param>
    public ConfigureContextualOptions(Action<IOptionsContext, TOptions> configureOptions, IOptionsContext context)
    {
        ConfigureOptions = configureOptions;
        _context = context;
    }

    /// <summary>
    /// Gets the delegate used to configure options instances.
    /// </summary>
    public Action<IOptionsContext, TOptions> ConfigureOptions { get; }

    /// <inheritdoc/>
    public void Configure(TOptions options) => ConfigureOptions(_context, Throw.IfNull(options));

    /// <summary>
    /// Does nothing.
    /// </summary>
    public void Dispose()
    {
        // Method intentionally left empty.
    }
}

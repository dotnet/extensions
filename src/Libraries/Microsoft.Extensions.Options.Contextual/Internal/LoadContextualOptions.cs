// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options.Contextual.Provider;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Options.Contextual.Internal;

/// <summary>
/// Used to retrieve named configuration data from a contextual options provider implementation.
/// </summary>
/// <typeparam name="TOptions">The type of options configured.</typeparam>
internal sealed class LoadContextualOptions<TOptions> : ILoadContextualOptions<TOptions>
    where TOptions : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoadContextualOptions{TOptions}"/> class.
    /// </summary>
    /// <param name="name">The name of the options instance being configured. If null, this instance will configure options with any name.</param>
    /// <param name="load">The delegate used to load configuration data.</param>
    public LoadContextualOptions(string? name, Func<IOptionsContext, CancellationToken, ValueTask<IConfigureContextualOptions<TOptions>>> load)
    {
        Name = name;
        LoadAction = load;
    }

    /// <summary>
    /// Gets the name of the options instance this object configures.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the delegate used to load configuration data.
    /// </summary>
    public Func<IOptionsContext, CancellationToken, ValueTask<IConfigureContextualOptions<TOptions>>> LoadAction { get; }

    /// <inheritdoc/>
    public ValueTask<IConfigureContextualOptions<TOptions>> LoadAsync<TContext>(string name, in TContext context, CancellationToken cancellationToken)
        where TContext : notnull, IOptionsContext
    {
        _ = Throw.IfNull(name);
        _ = Throw.IfNull(context);

        if (Name == null || name == Name)
        {
            return LoadAction(context, cancellationToken);
        }

        return new ValueTask<IConfigureContextualOptions<TOptions>>(NullConfigureContextualOptions.GetInstance<TOptions>());
    }
}

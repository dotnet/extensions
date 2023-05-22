// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Options.Contextual;

/// <summary>
/// Implementation of <see cref="IPostConfigureContextualOptions{TOptions}"/>.
/// </summary>
/// <typeparam name="TOptions">Options type being configured.</typeparam>
internal sealed class PostConfigureContextualOptions<TOptions> : IPostConfigureContextualOptions<TOptions>
    where TOptions : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostConfigureContextualOptions{TOptions}"/> class.
    /// </summary>
    /// <param name="name">The name of the options.</param>
    /// <param name="action">The action to register.</param>
    public PostConfigureContextualOptions(string? name, Action<IOptionsContext, TOptions> action)
    {
        Name = name;
        Action = action;
    }

    /// <summary>
    /// Gets the options name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the initialization action.
    /// </summary>
    public Action<IOptionsContext, TOptions> Action { get; }

    /// <inheritdoc/>
    public void PostConfigure<TContext>(string name, in TContext context, TOptions options)
        where TContext : notnull, IOptionsContext
    {
        _ = Throw.IfNull(name);
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(options);

        if (Name == null || name == Name)
        {
            Action(context, options);
        }
    }
}

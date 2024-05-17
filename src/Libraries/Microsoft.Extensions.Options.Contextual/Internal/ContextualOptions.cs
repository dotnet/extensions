// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Options.Contextual.Internal;

/// <summary>
/// Used to retrieve configured TOptions instances based on a context.
/// </summary>
/// <typeparam name="TOptions">The type of options being requested.</typeparam>
/// <typeparam name="TContext">A type defining the context for this request.</typeparam>
internal sealed class ContextualOptions<TOptions, TContext> : INamedContextualOptions<TOptions, TContext>
    where TOptions : class
    where TContext : notnull, IOptionsContext
{
    private readonly IContextualOptionsFactory<TOptions> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextualOptions{TOptions, TContext}"/> class.
    /// </summary>
    /// <param name="factory">The factory to create instances of <typeparamref name="TOptions"/> with.</param>
    public ContextualOptions(IContextualOptionsFactory<TOptions> factory)
    {
        _factory = factory;
    }

    /// <inheritdoc/>
    public ValueTask<TOptions> GetAsync(in TContext context, CancellationToken cancellationToken)
        => GetAsync(Options.DefaultName, context, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<TOptions> GetAsync(string name, in TContext context, CancellationToken cancellationToken)
        => _factory.CreateAsync(Throw.IfNull(name), context, cancellationToken);
}

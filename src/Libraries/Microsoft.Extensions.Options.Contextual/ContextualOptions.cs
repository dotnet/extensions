// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Options.Contextual;

/// <summary>
/// Used to retrieve configured TOptions instances based on a context.
/// </summary>
/// <typeparam name="TOptions">The type of options being requested.</typeparam>
internal sealed class ContextualOptions<TOptions> : INamedContextualOptions<TOptions>
    where TOptions : class
{
    private readonly IContextualOptionsFactory<TOptions> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextualOptions{TOptions}"/> class.
    /// </summary>
    /// <param name="factory">The factory to create instances of <typeparamref name="TOptions"/> with.</param>
    public ContextualOptions(IContextualOptionsFactory<TOptions> factory)
    {
        _factory = factory;
    }

    /// <inheritdoc/>
    public ValueTask<TOptions> GetAsync<TContext>(in TContext context, CancellationToken cancellationToken)
        where TContext : notnull, IOptionsContext
        => GetAsync(Microsoft.Extensions.Options.Options.DefaultName, context, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<TOptions> GetAsync<TContext>(string name, in TContext context, CancellationToken cancellationToken)
        where TContext : notnull, IOptionsContext
        => _factory.CreateAsync(Throw.IfNull(name), context, cancellationToken);
}

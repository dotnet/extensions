// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Options.Contextual;

#pragma warning disable SA1649 // File name should match first type name

/// <summary>
/// A do-nothing implementation of <see cref="IConfigureContextualOptions{TOptions}"/>.
/// </summary>
/// <typeparam name="TOptions">The options type to configure.</typeparam>
internal sealed class NullConfigureContextualOptions<TOptions> : IConfigureContextualOptions<TOptions>
    where TOptions : class
{
    internal static IConfigureContextualOptions<TOptions> Instance { get; } = new NullConfigureContextualOptions<TOptions>();

    /// <inheritdoc/>
    public void Configure(TOptions options)
    {
        // Method intentionally left empty.
    }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public void Dispose()
    {
        // Method intentionally left empty.
    }
}

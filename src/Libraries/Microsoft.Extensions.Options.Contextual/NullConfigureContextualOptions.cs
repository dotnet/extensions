// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Options.Contextual;

/// <summary>
/// Helper class.
/// </summary>
public static class NullConfigureContextualOptions
{
    /// <summary>
    /// Gets a singleton instance of an empty configuration context.
    /// </summary>
    /// <typeparam name="TOptions">The options type to configure.</typeparam>
    /// <returns>A do-nothing instance of <see cref="IConfigureContextualOptions{TOptions}"/>.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S4049:Properties should be preferred", Justification = "Not possible for generic methods.")]
    public static IConfigureContextualOptions<TOptions> GetInstance<TOptions>()
        where TOptions : class
        => NullConfigureContextualOptions<TOptions>.Instance;
}

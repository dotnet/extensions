// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Options.Contextual;

/// <summary>
/// The context used to configure contextual options.
/// </summary>
public interface IOptionsContext
{
    /// <summary>
    /// Passes context data to a contextual options provider.
    /// </summary>
    /// <typeparam name="T">The type that the contextual options provider uses to collect context.</typeparam>
    /// <param name="receiver">The object that the contextual options provider uses to collect the context.</param>
    void PopulateReceiver<T>(T receiver)
        where T : IOptionsContextReceiver;
}

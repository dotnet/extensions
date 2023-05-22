// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Resilience.Internal;

/// <summary>
/// Interface allowing registration of unique listeners for a given type of options.
/// </summary>
internal interface IOnChangeListenersHandler : IDisposable
{
    /// <summary>
    /// Captures the OnChange event and stores the associated listener for the options instance
    /// of type <typeparamref name="TOptions"/> named <paramref name="optionsName"/> ensuring only
    /// one listener per name and type is created.
    /// </summary>
    /// <typeparam name="TOptions">Type of the options.</typeparam>
    /// <param name="optionsName">The name of the options.</param>
    /// <returns>true if a listener for the same options type and name was not previously registered, otherwise false.</returns>
    public bool TryCaptureOnChange<TOptions>(string optionsName);
}

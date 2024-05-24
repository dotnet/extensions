// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Options.Contextual.Provider;

/// <summary>
/// Provides functionality to configure the <typeparamref name="TOptions"/> type.
/// </summary>
/// <typeparam name="TOptions">The type of options configured.</typeparam>
public interface IConfigureContextualOptions<in TOptions> : IDisposable
    where TOptions : class
{
    /// <summary>
    /// Configures a <typeparamref name="TOptions"/> instance.
    /// </summary>
    /// <param name="options">The options instance to configure.</param>
    void Configure(TOptions options);
}

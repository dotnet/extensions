// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting.Testing;

/// <summary>
/// Holds the initialization function, so we can pass it through <see cref="IServiceProvider"/>.
/// </summary>
public interface IStartupInitializer
{
    /// <summary>
    /// Short startup initialization job.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>New <see cref="Task"/>.</returns>
    public Task InitializeAsync(CancellationToken token);
}

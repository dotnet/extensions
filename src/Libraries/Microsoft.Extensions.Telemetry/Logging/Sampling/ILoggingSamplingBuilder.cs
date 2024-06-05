// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// An interface for configuring logging sampling.
/// </summary>
public interface ILoggingSamplingBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> where logging sampling services are configured.
    /// </summary>
    public IServiceCollection Services { get; }
}

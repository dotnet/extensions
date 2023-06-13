// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting.Testing;

/// <summary>
/// Options to configure <see cref="FakeHost"/>.
/// </summary>
public class FakeHostOptions
{
    /// <summary>
    /// Gets or sets time limit for host to start.
    /// </summary>
    /// <remarks>Default is 5 seconds. This limit is used if no cancellation token is used by user.</remarks>
    public TimeSpan StartUpTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets time limit for host to shut down.
    /// </summary>
    /// <remarks>Default is 10 seconds. This limit is used if no cancellation token is used by user.</remarks>
    public TimeSpan ShutDownTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets time limit for host to be up.
    /// </summary>
    /// <remarks>
    /// Default is 30 seconds.
    /// Value -1 millisecond means infinite time to live.
    /// TimeToLive is not enforced when debugging.
    /// </remarks>
    public TimeSpan TimeToLive { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether fake logging is configured automatically.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool FakeLogging { get; set; } = true;

    /// <inheritdoc cref="ServiceProviderOptions"/>
    public bool ValidateScopes { get; set; } = true;

    /// <inheritdoc cref="ServiceProviderOptions"/>
    public bool ValidateOnBuild { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether fake redaction is configured automatically.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool FakeRedaction { get; set; } = true;
}

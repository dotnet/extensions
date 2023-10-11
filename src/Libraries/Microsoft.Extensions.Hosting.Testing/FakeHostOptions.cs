// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Hosting.Testing;

/// <summary>
/// Options to configure <see cref="FakeHost"/>.
/// </summary>
[Experimental(diagnosticId: Experiments.Hosting, UrlFormat = Experiments.UrlFormat)]
public class FakeHostOptions
{
    /// <summary>
    /// Gets or sets the time limit for the host to start.
    /// </summary>
    /// <value>The default value is 5 seconds.</value>
    /// <remarks>This limit is used if there's no cancellation token.</remarks>
    public TimeSpan StartUpTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the time limit for the host to shut down.
    /// </summary>
    /// <value>The default value is 10 seconds.</value>
    /// <remarks>This limit is used if there's no cancellation token.</remarks>
    public TimeSpan ShutDownTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the time limit for the host to be up.
    /// </summary>
    /// <value>The default is 30 seconds.</value>
    /// <remarks>
    /// -1 millisecond means infinite time to live.
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

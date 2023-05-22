// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Class for exception policy options definition.
/// </summary>
public class ExceptionPolicyOptions : ChaosPolicyOptionsBase
{
    /// <summary>
    /// The key for the default exception instance in the registry.
    /// </summary>
    internal const string DefaultExceptionKey = "DefaultException";

    /// <summary>
    /// Gets or sets the exception key.
    /// </summary>
    /// <remarks>
    /// This key is used for fetching an exception instance
    /// from <see cref="IExceptionRegistry"/>.
    /// Default is set to "DefaultException".
    /// </remarks>
    public string ExceptionKey { get; set; } = DefaultExceptionKey;
}

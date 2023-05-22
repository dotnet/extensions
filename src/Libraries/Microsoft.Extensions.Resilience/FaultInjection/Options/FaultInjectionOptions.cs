// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Class to contain fault injection options provider option values loaded from configuration sources.
/// </summary>
public class FaultInjectionOptions
{
    /// <summary>
    /// Gets or sets the dictionary that stores <see cref="ChaosPolicyOptionsGroup"/>.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Options pattern.")]
    public IDictionary<string, ChaosPolicyOptionsGroup> ChaosPolicyOptionsGroups { get; set; } = new Dictionary<string, ChaosPolicyOptionsGroup>();
}

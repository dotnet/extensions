// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Hosting.Testing;

/// <summary>
/// Configures startup initialization logic.
/// </summary>
public class StartupInitializationOptions
{
    /// <summary>
    /// Gets or sets maximum time allowed for initialization logic.
    /// </summary>
    /// <value>
    /// The default value is 30 seconds.
    /// </value>
    [TimeSpan("00:00:05", "01:00:00")]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// Options to configure the connection timeout middleware.
/// </summary>
public class ConnectionTimeoutOptions
{
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the time after which a connection will be shut down.
    /// </summary>
    /// <value>
    /// The default value is 5 minutes.
    /// </value>
    [TimeSpan(0, Exclusive = true)]
    public TimeSpan Timeout { get; set; } = _defaultTimeout;
}

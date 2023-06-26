// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Polly.Timeout;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Implementation of the <see cref="TimeoutStrategyOptions"/> for HTTP scenarios.
/// </summary>
/// <remarks>
/// The default timeout is set to 30 seconds.
/// </remarks>
public class HttpTimeoutStrategyOptions : TimeoutStrategyOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpTimeoutStrategyOptions"/> class.
    /// </summary>
    public HttpTimeoutStrategyOptions()
    {
        Timeout = TimeSpan.FromSeconds(30);
    }
}

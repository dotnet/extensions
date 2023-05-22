// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Resilience.Options;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Static generators used within the current package.
/// </summary>
public static class HttpClientResilienceGenerators
{
    /// <summary>
    /// Gets the generator that is able to generate delay based on the "Retry-After" response header.
    /// </summary>
    public static readonly Func<RetryDelayArguments<HttpResponseMessage>, TimeSpan> HandleRetryAfterHeader = RetryAfterHelper.Generator;
}

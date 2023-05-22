// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Polly;
using Polly.Contrib.Simmy.Outcomes;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection;

/// <summary>
/// Factory for http response chaos policy creation.
/// </summary>
public interface IHttpClientChaosPolicyFactory
{
    /// <summary>
    /// Creates an async http response fault injection policy with delegate functions
    /// to fetch fault injection settings from <see cref="Context"/>.
    /// </summary>
    /// <returns>
    /// An http response fault injection policy,
    /// an instance of <see cref="AsyncInjectOutcomePolicy{HttpResponseMessage}"/>.
    /// </returns>
    public AsyncInjectOutcomePolicy<HttpResponseMessage> CreateHttpResponsePolicy();
}

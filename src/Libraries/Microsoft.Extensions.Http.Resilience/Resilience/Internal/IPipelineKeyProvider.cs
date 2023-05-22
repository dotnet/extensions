// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// The provider that returns the pipeline key from the request message.
/// </summary>
internal interface IPipelineKeyProvider
{
    /// <summary>
    /// Returns the pipeline key from the request message.
    /// </summary>
    /// <param name="requestMessage">The request message.</param>
    /// <returns>The pipeline key.</returns>
    string GetPipelineKey(HttpRequestMessage requestMessage);
}

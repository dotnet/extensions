// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Internal;

internal sealed class HttpContentOptionsRegistry : IHttpContentOptionsRegistry
{
    private readonly IOptionsMonitor<HttpContentOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpContentOptionsRegistry"/> class.
    /// </summary>
    /// <param name="options">
    /// The <see cref="IOptionsMonitor{TOptions}"/> instance to retrieve <see cref="HttpContentOptions"/> from.
    /// </param>
    public HttpContentOptionsRegistry(IOptionsMonitor<HttpContentOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc/>
    public HttpContent? GetHttpContent(string key)
    {
        return _options.Get(key).HttpContent;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Internal;

internal sealed class HttpContentOptions
{
    public HttpContent? HttpContent { get; set; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// The provider that returns the pipeline key from the request message.
/// </summary>
internal sealed class PipelineKeyOptions
{
    public Func<HttpRequestMessage, string>? KeyProvider { get; set; }
}

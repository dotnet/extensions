// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test.Internal;

internal interface ITestHttpClient1
{
    Task<HttpResponseMessage> SendRequest(HttpRequestMessage httpRequestMessage);
}

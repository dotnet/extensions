// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Http.Logging.Test.Internal;

public class TestHttpClient1 : ITestHttpClient1
{
    private readonly System.Net.Http.HttpClient _httpClient;

    public TestHttpClient1(System.Net.Http.HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<HttpResponseMessage> SendRequest(HttpRequestMessage httpRequestMessage)
    {
        return _httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Telemetry.Logging.Test.Internal;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test;

public class HttpClientLoggingAcceptanceTest
{
    [Theory]
    [InlineData(4_096)]
    [InlineData(8_192)]
    [InlineData(16_384)]
    [InlineData(32_768)]
    [InlineData(315_883)]
    public async Task HttpClientLoggingHandler_LogsBodyDataUpToSpecifiedLimit(int limit)
    {
        const string RequestPath = "https://we.wont.hit.this.dd22anyway.com";

        await using var provider = new ServiceCollection()
             .AddFakeLogging()
             .AddFakeRedaction()
             .AddHttpClient(nameof(HttpClientLoggingHandler_LogsBodyDataUpToSpecifiedLimit))
             .AddHttpClientLogging(x =>
             {
                 x.ResponseHeadersDataClasses.Add("ResponseHeader", SimpleClassifications.PrivateData);
                 x.RequestHeadersDataClasses.Add("RequestHeader", SimpleClassifications.PrivateData);
                 x.RequestHeadersDataClasses.Add("RequestHeader2", SimpleClassifications.PrivateData);
                 x.RequestBodyContentTypes.Add("application/json");
                 x.ResponseBodyContentTypes.Add("application/json");
                 x.BodySizeLimit = limit;
                 x.LogBody = true;
             })
             .Services
             .BlockRemoteCall()
             .BuildServiceProvider();

        var client = provider
             .GetRequiredService<IHttpClientFactory>()
             .CreateClient(nameof(HttpClientLoggingHandler_LogsBodyDataUpToSpecifiedLimit));
        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(RequestPath),
        };
        httpRequestMessage.Headers.Add("requestHeader", "Request Value");
        httpRequestMessage.Headers.Add("ReQuEStHeAdEr2", new List<string> { "Request Value 2", "Request Value 3" });

        var content = await client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        var responseStream = await content.Content.ReadAsStreamAsync();
        var length = (int)responseStream.Length > limit ? limit : (int)responseStream.Length;
        var buffer = new byte[length];
        _ = await responseStream.ReadAsync(buffer, 0, length);
        var responseString = Encoding.UTF8.GetString(buffer);

        var collector = provider.GetFakeLogCollector();
        var logRecord = collector.GetSnapshot().Single(l => l.Category == "Microsoft.Extensions.Http.Telemetry.Logging.HttpClientLogger");
        var state = logRecord.State as List<KeyValuePair<string, string>>;
        state.Should().Contain(kvp => kvp.Value == responseString);
        state.Should().Contain(kvp => kvp.Value == "Request Value");
        state.Should().Contain(kvp => kvp.Value == "Request Value 2,Request Value 3");
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Http.Logging.Internal;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.Http.Logging.Test;

public class LoggerMessageStateExtensionsTests
{
    [Fact]
    public void AddRequestHeadersTest()
    {
        AddHeadersTest(
            LoggerMessageStateExtensions.AddRequestHeaders,
            HttpClientLoggingTagNames.RequestHeaderPrefix);
    }

    [Fact]
    public void AddResponseHeadersTest()
    {
        AddHeadersTest(
            LoggerMessageStateExtensions.AddResponseHeaders,
            HttpClientLoggingTagNames.ResponseHeaderPrefix);
    }

    private static void AddHeadersTest(AddHeadersDelegate addHeaders, string prefix)
    {
        const string Header1 = "Accept-Charset";
        const string Header2 = "CONTENT-TYPE";

        const string NormalizedHeader1 = "accept_charset";
        const string NormalizedHeader2 = "content_type";

        List<KeyValuePair<string, string>> headers = [new(Header1, "v1"), new(Header2, "v2")];

        var state = new LoggerMessageState();
        int index = 0;

        state.ReserveTagSpace(2);
        addHeaders(state, headers, ref index);

        Assert.Equal(prefix + NormalizedHeader1, state.TagArray[0].Key);
        Assert.Equal("v1", state.TagArray[0].Value);

        Assert.Equal(prefix + NormalizedHeader2, state.TagArray[1].Key);
        Assert.Equal("v2", state.TagArray[1].Value);
    }

    private delegate void AddHeadersDelegate(LoggerMessageState state, List<KeyValuePair<string, string>> items, ref int index);
}

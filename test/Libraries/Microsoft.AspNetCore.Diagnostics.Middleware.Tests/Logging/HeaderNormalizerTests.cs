// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Classification;
using Xunit;

public class HeaderNormalizerTests
{
    [Fact]
    public void PrepareNormalizedHeaderNamesTest()
    {
        const string Prefix = "prefix.";

        var headers = HeaderNormalizer.PrepareNormalizedHeaderNames(new[]
            {
                new KeyValuePair<string, DataClassification>("Accept-Charset", DataClassification.Unknown),
                new KeyValuePair<string, DataClassification>("CONTENT-TYPE", DataClassification.Unknown)
            },
            Prefix);

        Assert.Equal(2, headers.Length);
        Assert.Equal(Prefix + "accept_charset", headers[0]);
        Assert.Equal(Prefix + "content_type", headers[1]);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Telemetry.RequestHeaders.Test.Internals;

internal static class TestExtensions
{
    public static IOptions<RequestHeadersLogEnricherOptions> ToOptions(this RequestHeadersLogEnricherOptions options)
    {
        var mock = new Mock<IOptions<RequestHeadersLogEnricherOptions>>();
        mock.Setup(o => o.Value).Returns(options);
        return mock.Object;
    }
}

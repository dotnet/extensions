// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.Extensions.Telemetry.Enrichment.Service.Test.Internals;

internal static class TestExtensions
{
    public static IOptions<T> ToOptions<T>(this T options)
        where T : class, new()
    {
        var mock = new Mock<IOptions<T>>();
        mock.Setup(o => o.Value).Returns(options);
        return mock.Object;
    }
}

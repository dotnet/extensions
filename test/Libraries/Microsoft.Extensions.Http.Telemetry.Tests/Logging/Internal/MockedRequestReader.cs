// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.Telemetry.Logging.Internal;
using Moq;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test.Internal;

internal class MockedRequestReader : IHttpRequestReader
{
    internal Mock<IHttpRequestReader> Mock { get; }

    internal MockedRequestReader(Mock<IHttpRequestReader> mock)
    {
        Mock = mock;
    }

    public Task ReadResponseAsync(LogRecord record,
        HttpResponseMessage response,
        List<KeyValuePair<string, string>>? buffer,
        CancellationToken cancellationToken) => Mock.Object.ReadResponseAsync(record, response, buffer, cancellationToken);

    public Task ReadRequestAsync(LogRecord record,
        HttpRequestMessage request,
        List<KeyValuePair<string, string>>? buffer,
        CancellationToken cancellationToken) => Mock.Object.ReadRequestAsync(record, request, buffer, cancellationToken);
}

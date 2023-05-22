// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Moq;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Hedging.Internals;

public class RequestMessageSnapshotPolicyTests
{
    [Fact]
    public async Task SendAsync_EnsureSnapshotAttached()
    {
        var snapshot = new Mock<IHttpRequestMessageSnapshot>(MockBehavior.Strict);
        snapshot.Setup(s => s.Dispose());
        var cloner = new Mock<IRequestClonerInternal>(MockBehavior.Strict);
        cloner.Setup(c => c.CreateSnapshot(It.IsAny<HttpRequestMessage>())).Returns(snapshot.Object);
        var policy = new RequestMessageSnapshotPolicy("dummy", cloner.Object);
        var context = new Context
        {
            ["Resilience.ContextExtensions.Request-dummy"] = new HttpRequestMessage()
        };

        await policy.ExecuteAsync(_ => Task.FromResult(new HttpResponseMessage()), context);

        HedgingContextExtensions.CreateRequestMessageSnapshotProvider("dummy")(context).Should().Be(snapshot.Object);
        cloner.VerifyAll();
        snapshot.VerifyAll();
    }
}

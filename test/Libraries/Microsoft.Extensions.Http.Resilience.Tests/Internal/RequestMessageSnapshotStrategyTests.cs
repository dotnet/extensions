// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Moq;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Internal;

public class RequestMessageSnapshotStrategyTests
{
    [Fact]
    public async Task SendAsync_EnsureSnapshotAttached()
    {
        var snapshot = new Mock<IHttpRequestMessageSnapshot>(MockBehavior.Strict);
        snapshot.Setup(s => s.Dispose());
        var cloner = new Mock<IRequestCloner>(MockBehavior.Strict);
        cloner.Setup(c => c.CreateSnapshot(It.IsAny<HttpRequestMessage>())).Returns(snapshot.Object);
        var strategy = new RequestMessageSnapshotStrategy(cloner.Object);
        var context = ResilienceContext.Get();
        using var request = new HttpRequestMessage();
        context.Properties.Set(ResilienceKeys.RequestMessage, request);

        using var response = await strategy.ExecuteAsync(
            context =>
            {
                context.Properties.GetValue(ResilienceKeys.RequestSnapshot, null!).Should().Be(snapshot.Object);
                return new ValueTask<HttpResponseMessage>(new HttpResponseMessage());
            },
            context);

        cloner.VerifyAll();
        snapshot.VerifyAll();
    }

    [Fact]
    public void ExecuteAsync_requestMessageNotFound_Throws()
    {
        var strategy = new RequestMessageSnapshotStrategy(Mock.Of<IRequestCloner>());

        strategy.Invoking(s => s.Execute(() => { })).Should().Throw<InvalidOperationException>();
    }
}

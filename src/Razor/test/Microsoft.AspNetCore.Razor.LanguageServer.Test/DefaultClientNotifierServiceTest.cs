// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Moq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test
{
    public class DefaultClientNotifierServiceTest
    {
        [Fact]
        public async Task SendRequestAsync_TaskCompletes_LanguageServerIsCalled()
        {
            // Arrange
            var method = "anystring";
            var responseRouterReturns = new Mock<IResponseRouterReturns>(MockBehavior.Strict);

            var taskCompletionSource = new TaskCompletionSource<bool>();
            var languageServer = new Mock<IClientLanguageServer>(MockBehavior.Strict);
            languageServer.Setup(l => l.SendRequest(method))
                .Returns(responseRouterReturns.Object);

            var notifierServer = new DefaultClientNotifierService(languageServer.Object, taskCompletionSource);

            // Act
            taskCompletionSource.SetResult(true);
            var result = await notifierServer.SendRequestAsync(method);

            // Assert
            Assert.Equal(responseRouterReturns.Object, result);
            languageServer.VerifyAll();
        }

        [Fact]
        public async Task SendRequestAsync_TaskDoesNotComplete_LanguageServerNeverHit()
        {
            // Arrange
            var method = "anystring";
            var responseRouterReturns = new Mock<IResponseRouterReturns>(MockBehavior.Strict);

            var taskCompletionSource = new TaskCompletionSource<bool>();
            var languageServer = new Mock<IClientLanguageServer>(MockBehavior.Strict);

            var notifierServer = new DefaultClientNotifierService(languageServer.Object, taskCompletionSource);

            // Act & Assert
            taskCompletionSource.SetException(new ArgumentNullException());
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await notifierServer.SendRequestAsync(method));

            languageServer.VerifyAll();
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
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

            var languageServer = new Mock<IClientLanguageServer>(MockBehavior.Strict);
            languageServer.Setup(l => l.SendRequest(method))
                .Returns(responseRouterReturns.Object);

            var notifierServer = new DefaultClientNotifierService(languageServer.Object);

            // Act
            await notifierServer.OnStarted(server:null, CancellationToken.None);
            var result = await notifierServer.SendRequestAsync(method);

            // Assert
            Assert.Equal(responseRouterReturns.Object, result);
            languageServer.VerifyAll();
        }

        [Fact]
        public void SendRequestAsync_TaskDoesNotComplete_LanguageServerNeverHit()
        {
            // Arrange
            var method = "anystring";

            var languageServer = new Mock<IClientLanguageServer>(MockBehavior.Strict);

            var notifierServer = new DefaultClientNotifierService(languageServer.Object);

            // Act & Assert
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            Assert.False(notifierServer.SendRequestAsync(method).Wait(TimeSpan.FromSeconds(2)));
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

            languageServer.VerifyAll();
        }
    }
}

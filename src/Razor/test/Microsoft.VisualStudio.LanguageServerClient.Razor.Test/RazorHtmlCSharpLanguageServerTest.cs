// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class RazorHtmlCSharpLanguageServerTest
    {
        [Fact]
        public async Task ExecuteRequestAsync_InvokesCustomHandler()
        {
            // Arrange
            var handler = new Mock<IRequestHandler<string, int>>();
            handler.Setup(h => h.HandleRequestAsync("hello world", It.IsAny<ClientCapabilities>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(123))
                .Verifiable();
            var metadata = Mock.Of<IRequestHandlerMetadata>(rhm => rhm.MethodName == "test");
            using var languageServer = new RazorHtmlCSharpLanguageServer(new[] { new Lazy<IRequestHandler, IRequestHandlerMetadata>(() => handler.Object, metadata) });

            // Act
            var result = await languageServer.ExecuteRequestAsync<string, int>("test", "hello world", clientCapabilities: null, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(123, result);
            handler.VerifyAll();
        }

        [Fact]
        public async Task InitializeAsync_InvokesHandlerWithParamsAndCapabilities()
        {
            // Arrange
            var originalInitParams = new InitializeParams()
            {
                Capabilities = new ClientCapabilities()
                {
                    Experimental = true
                },
                RootUri = new Uri("C:/path/to/workspace"),
            };
            var initializeResult = new InitializeResult();
            var handler = new Mock<IRequestHandler<InitializeParams, InitializeResult>>();
            handler.Setup(h => h.HandleRequestAsync(It.IsAny<InitializeParams>(), It.IsAny<ClientCapabilities>(), It.IsAny<CancellationToken>()))
                .Callback<InitializeParams, ClientCapabilities, CancellationToken>((initParams, clientCapabilities, token) =>
                {
                    Assert.True((bool)initParams.Capabilities.Experimental);
                    Assert.Equal(originalInitParams.RootUri.AbsoluteUri, initParams.RootUri.AbsoluteUri);
                })
                .Returns(Task.FromResult(initializeResult))
                .Verifiable();
            var metadata = Mock.Of<IRequestHandlerMetadata>(rhm => rhm.MethodName == Methods.InitializeName);
            using var languageServer = new RazorHtmlCSharpLanguageServer(new[] { new Lazy<IRequestHandler, IRequestHandlerMetadata>(() => handler.Object, metadata) });
            var serializedInitParams = JToken.FromObject(originalInitParams);

            // Act
            var result = await languageServer.InitializeAsync(serializedInitParams, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Same(initializeResult, result);
            handler.VerifyAll();
        }
    }
}

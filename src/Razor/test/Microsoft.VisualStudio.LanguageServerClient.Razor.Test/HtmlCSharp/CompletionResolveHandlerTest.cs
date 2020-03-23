// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public class CompletionResolveHandlerTest
    {
        [Fact]
        public async Task HandleRequestAsync_InvokesCSharpLanguageServer()
        {
            // Arrange
            var called = false;
            var originalData = new object();
            var request = new CompletionItem()
            {
                InsertText = "DateTime",
                Data = new CompletionResolveData() { LanguageServerKind = LanguageServerKind.CSharp, OriginalData = originalData }
            };
            var expectedResponse = new CompletionItem()
            {
                InsertText = "DateTime",
                Data = originalData,
                Detail = "Some documentation"
            };

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.RequestServerAsync<CompletionItem, CompletionItem>(It.IsAny<string>(), LanguageServerKind.CSharp, It.IsAny<CompletionItem>(), It.IsAny<CancellationToken>()))
                .Callback<string, LanguageServerKind, CompletionItem, CancellationToken>((method, serverKind, completionItem, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionResolveName, method);
                    Assert.Equal(LanguageServerKind.CSharp, serverKind);
                    Assert.Same(originalData, completionItem.Data);
                    called = true;
                })
                .Returns(Task.FromResult<CompletionItem>(expectedResponse));

            var handler = new CompletionResolveHandler(requestInvoker.Object);

            // Act
            var result = await handler.HandleRequestAsync(request, new ClientCapabilities(), CancellationToken.None);

            // Assert
            Assert.True(called);
            Assert.Same(expectedResponse, result);
        }

        [Fact]
        public async Task HandleRequestAsync_DoesNotInvokeHtmlLanguageServer()
        {
            // Arrange
            var originalData = new object();
            var request = new CompletionItem()
            {
                InsertText = "div",
                Data = new CompletionResolveData() { LanguageServerKind = LanguageServerKind.Html, OriginalData = originalData }
            };

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);

            var handler = new CompletionResolveHandler(requestInvoker.Object);

            // Act
            var result = await handler.HandleRequestAsync(request, new ClientCapabilities(), CancellationToken.None);

            // Assert (Does not throw with MockBehavior.Strict)
            Assert.Equal("div", result.InsertText);
            Assert.Same(originalData, result.Data);
        }
    }
}

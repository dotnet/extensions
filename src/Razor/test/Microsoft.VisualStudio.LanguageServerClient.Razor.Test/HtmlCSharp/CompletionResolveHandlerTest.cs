// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
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
                .Setup(r => r.ReinvokeRequestOnServerAsync<CompletionItem, CompletionItem>(It.IsAny<string>(), RazorLSPConstants.CSharpContentTypeName, It.IsAny<CompletionItem>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CompletionItem, CancellationToken>((method, serverContentType, completionItem, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionResolveName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    Assert.Same(originalData, completionItem.Data);
                    called = true;
                })
                .Returns(Task.FromResult<CompletionItem>(expectedResponse));

            var handler = new CompletionResolveHandler(requestInvoker.Object);

            // Act
            var result = await handler.HandleRequestAsync(request, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.Same(expectedResponse, result);
        }

        [Fact]
        public async Task HandleRequestAsync_InvokesHtmlLanguageServer()
        {
            // Arrange
            var called = false;
            var originalData = new object();
            var request = new CompletionItem()
            {
                InsertText = "strong",
                Data = new CompletionResolveData() { LanguageServerKind = LanguageServerKind.Html, OriginalData = originalData }
            };
            var expectedResponse = new CompletionItem()
            {
                InsertText = "strong",
                Data = originalData,
                Detail = "Some documentation"
            };

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<CompletionItem, CompletionItem>(It.IsAny<string>(), RazorLSPConstants.HtmlLSPContentTypeName, It.IsAny<CompletionItem>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CompletionItem, CancellationToken>((method, serverContentType, completionItem, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionResolveName, method);
                    Assert.Equal(RazorLSPConstants.HtmlLSPContentTypeName, serverContentType);
                    Assert.Same(originalData, completionItem.Data);
                    called = true;
                })
                .Returns(Task.FromResult<CompletionItem>(expectedResponse));

            var handler = new CompletionResolveHandler(requestInvoker.Object);

            // Act
            var result = await handler.HandleRequestAsync(request, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.Same(expectedResponse, result);
        }
    }
}

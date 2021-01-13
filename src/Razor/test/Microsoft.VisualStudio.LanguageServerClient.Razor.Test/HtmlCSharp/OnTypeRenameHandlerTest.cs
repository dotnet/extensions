// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Moq;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public class OnTypeRenameHandlerTest
    {
        private static readonly Uri Uri = new Uri("C:/path/to/file.razor");

        [Fact]
        public async Task HandleRequestAsync_DocumentNotFound_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var requestInvoker = Mock.Of<LSPRequestInvoker>();
            var projectionProvider = Mock.Of<LSPProjectionProvider>();
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>();
            var onTypeRenameHandler = new OnTypeRenameHandler(documentManager, requestInvoker, projectionProvider, documentMappingProvider);
            var onTypeRenameRequest = new DocumentOnTypeRenameParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1)
            };

            // Act
            var result = await onTypeRenameHandler.HandleRequestAsync(onTypeRenameRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_ProjectionNotFound_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>());
            var requestInvoker = Mock.Of<LSPRequestInvoker>();
            var projectionProvider = Mock.Of<LSPProjectionProvider>();
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>();
            var onTypeRenameHandler = new OnTypeRenameHandler(documentManager, requestInvoker, projectionProvider, documentMappingProvider);
            var onTypeRenameRequest = new DocumentOnTypeRenameParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1)
            };

            // Act
            var result = await onTypeRenameHandler.HandleRequestAsync(onTypeRenameRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 0));
            var requestInvoker = Mock.Of<LSPRequestInvoker>();
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>();

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = GetProjectionProvider(projectionResult);

            var onTypeRenameHandler = new OnTypeRenameHandler(documentManager, requestInvoker, projectionProvider, documentMappingProvider);
            var onTypeRenameRequest = new DocumentOnTypeRenameParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await onTypeRenameHandler.HandleRequestAsync(onTypeRenameRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_HtmlProjection_RemapsHighlightRange()
        {
            // Arrange
            var invokerCalled = false;
            var expectedResponse = GetMatchingHTMLBracketRange(5);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 0));

            var htmlResponse = GetMatchingHTMLBracketRange(10);
            var requestInvoker = GetRequestInvoker<DocumentOnTypeRenameParams, DocumentOnTypeRenameResponseItem>(
                htmlResponse,
                (method, serverContentType, highlightParams, ct) =>
                {
                    Assert.Equal(MSLSPMethods.OnTypeRenameName, method);
                    Assert.Equal(RazorLSPConstants.HtmlLSPContentTypeName, serverContentType);
                    invokerCalled = true;
                });

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
            };
            var projectionProvider = GetProjectionProvider(projectionResult);

            var documentMappingProvider = GetDocumentMappingProvider(expectedResponse.Ranges, 0, RazorLanguageKind.Html);

            var onTypeRenameHandler = new OnTypeRenameHandler(documentManager, requestInvoker, projectionProvider, documentMappingProvider);
            var onTypeRenameRequest = new DocumentOnTypeRenameParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await onTypeRenameHandler.HandleRequestAsync(onTypeRenameRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(invokerCalled);
            Assert.NotNull(result);
            Assert.Collection(result.Ranges,
                r => Assert.Equal(expectedResponse.Ranges[0], r),
                r => Assert.Equal(expectedResponse.Ranges[1], r));
        }

        [Fact]
        public async Task HandleRequestAsync_VersionMismatch_ReturnsNull()
        {
            // Arrange
            var invokerCalled = false;
            var expectedResponse = GetMatchingHTMLBracketRange(5);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 1));

            var htmlResponse = GetMatchingHTMLBracketRange(10);
            var requestInvoker = GetRequestInvoker<DocumentOnTypeRenameParams, DocumentOnTypeRenameResponseItem>(
                htmlResponse,
                (method, serverContentType, highlightParams, ct) =>
                {
                    Assert.Equal(MSLSPMethods.OnTypeRenameName, method);
                    Assert.Equal(RazorLSPConstants.HtmlLSPContentTypeName, serverContentType);
                    invokerCalled = true;
                });

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
            };
            var projectionProvider = GetProjectionProvider(projectionResult);

            var documentMappingProvider = GetDocumentMappingProvider(expectedResponse.Ranges, 0 /* Different from document version (1) */, RazorLanguageKind.Html);

            var onTypeRenameHandler = new OnTypeRenameHandler(documentManager, requestInvoker, projectionProvider, documentMappingProvider);
            var onTypeRenameRequest = new DocumentOnTypeRenameParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await onTypeRenameHandler.HandleRequestAsync(onTypeRenameRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(invokerCalled);
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_RemapFailure_ReturnsNull()
        {
            // Arrange
            var invokerCalled = false;
            var expectedResponse = GetMatchingHTMLBracketRange(5);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 0));

            var htmlResponse = GetMatchingHTMLBracketRange(10);
            var requestInvoker = GetRequestInvoker<DocumentOnTypeRenameParams, DocumentOnTypeRenameResponseItem>(
                htmlResponse,
                (method, serverContentType, highlightParams, ct) =>
                {
                    Assert.Equal(MSLSPMethods.OnTypeRenameName, method);
                    Assert.Equal(RazorLSPConstants.HtmlLSPContentTypeName, serverContentType);
                    invokerCalled = true;
                });

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
            };
            var projectionProvider = GetProjectionProvider(projectionResult);

            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>();

            var onTypeRenameHandler = new OnTypeRenameHandler(documentManager, requestInvoker, projectionProvider, documentMappingProvider);
            var onTypeRenameRequest = new DocumentOnTypeRenameParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await onTypeRenameHandler.HandleRequestAsync(onTypeRenameRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(invokerCalled);
            Assert.Null(result);
        }

        private LSPProjectionProvider GetProjectionProvider(ProjectionResult expectedResult)
        {
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(expectedResult));

            return projectionProvider.Object;
        }

        private LSPRequestInvoker GetRequestInvoker<TParams, TResult>(TResult expectedResponse, Action<string, string, TParams, CancellationToken> callback)
        {
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<TParams, TResult>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TParams>(), It.IsAny<CancellationToken>()))
                .Callback(callback)
                .Returns(Task.FromResult(expectedResponse));

            return requestInvoker.Object;
        }

        private LSPDocumentMappingProvider GetDocumentMappingProvider(Range[] expectedRanges, int expectedVersion, RazorLanguageKind languageKind)
        {
            var remappingResult = new RazorMapToDocumentRangesResponse()
            {
                Ranges = expectedRanges,
                HostDocumentVersion = expectedVersion
            };
            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(d => d.MapToDocumentRangesAsync(languageKind, Uri, It.IsAny<Range[]>(), It.IsAny<CancellationToken>())).
                Returns(Task.FromResult(remappingResult));

            return documentMappingProvider.Object;
        }

        private static DocumentOnTypeRenameResponseItem GetMatchingHTMLBracketRange(int line)
        {
            return new DocumentOnTypeRenameResponseItem()
            {
                Ranges = new[] {
                    new Range()
                    {
                        Start = new Position(line, 1),
                        End = new Position(line, 10)
                    },
                    new Range()
                    {
                        Start = new Position(line, 21),
                        End = new Position(line, 30)
                    }
                }
            };
        }
    }
}

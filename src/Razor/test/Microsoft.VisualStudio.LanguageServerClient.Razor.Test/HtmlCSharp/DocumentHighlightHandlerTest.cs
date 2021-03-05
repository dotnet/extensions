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
    public class DocumentHighlightHandlerTest : HandlerTestBase
    {
        public DocumentHighlightHandlerTest()
        {
            Uri = new Uri("C:/path/to/file.razor");
        }

        private Uri Uri { get; }

        [Fact]
        public async Task HandleRequestAsync_DocumentNotFound_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var requestInvoker = Mock.Of<LSPRequestInvoker>(MockBehavior.Strict);
            var projectionProvider = Mock.Of<LSPProjectionProvider>(MockBehavior.Strict);
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>(MockBehavior.Strict);
            var highlightHandler = new DocumentHighlightHandler(requestInvoker, documentManager, projectionProvider, documentMappingProvider, LoggerProvider);
            var highlightRequest = new DocumentHighlightParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1)
            };

            // Act
            var result = await highlightHandler.HandleRequestAsync(highlightRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_ProjectionNotFound_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(MockBehavior.Strict));
            var requestInvoker = Mock.Of<LSPRequestInvoker>(MockBehavior.Strict);
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict).Object;
            Mock.Get(projectionProvider).Setup(projectionProvider => projectionProvider.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), CancellationToken.None))
                .Returns(Task.FromResult<ProjectionResult>(null));
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>(MockBehavior.Strict);
            var highlightHandler = new DocumentHighlightHandler(requestInvoker, documentManager, projectionProvider, documentMappingProvider, LoggerProvider);
            var highlightRequest = new DocumentHighlightParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1)
            };

            // Act
            var result = await highlightHandler.HandleRequestAsync(highlightRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_RemapsHighlightRange()
        {
            // Arrange
            var called = false;
            var expectedHighlight = GetHighlight(5, 5, 5, 5);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 0, MockBehavior.Strict));

            var csharpHighlight = GetHighlight(100, 100, 100, 100);
            var requestInvoker = GetRequestInvoker<DocumentHighlightParams, DocumentHighlight[]>(
                new[] { csharpHighlight },
                (method, serverContentType, highlightParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentDocumentHighlightName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                });

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = GetProjectionProvider(projectionResult);

            var documentMappingProvider = GetDocumentMappingProvider(expectedHighlight.Range, 0, RazorLanguageKind.CSharp);

            var highlightHandler = new DocumentHighlightHandler(requestInvoker, documentManager, projectionProvider, documentMappingProvider, LoggerProvider);
            var highlightRequest = new DocumentHighlightParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await highlightHandler.HandleRequestAsync(highlightRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            var actualHighlight = Assert.Single(result);
            Assert.Equal(expectedHighlight.Range, actualHighlight.Range);
        }

        [Fact]
        public async Task HandleRequestAsync_HtmlProjection_RemapsHighlightRange()
        {
            // Arrange
            var called = false;
            var expectedHighlight = GetHighlight(5, 5, 5, 5);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 0, MockBehavior.Strict));

            var htmlHighlight = GetHighlight(100, 100, 100, 100);
            var requestInvoker = GetRequestInvoker<DocumentHighlightParams, DocumentHighlight[]>(
                new[] { htmlHighlight },
                (method, serverContentType, highlightParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentDocumentHighlightName, method);
                    Assert.Equal(RazorLSPConstants.HtmlLSPContentTypeName, serverContentType);
                    called = true;
                });

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
            };
            var projectionProvider = GetProjectionProvider(projectionResult);

            var documentMappingProvider = GetDocumentMappingProvider(expectedHighlight.Range, 0, RazorLanguageKind.Html);

            var highlightHandler = new DocumentHighlightHandler(requestInvoker, documentManager, projectionProvider, documentMappingProvider, LoggerProvider);
            var highlightRequest = new DocumentHighlightParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await highlightHandler.HandleRequestAsync(highlightRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            var actualHighlight = Assert.Single(result);
            Assert.Equal(expectedHighlight.Range, actualHighlight.Range);
        }

        [Fact]
        public async Task HandleRequestAsync_VersionMismatch_DiscardsLocation()
        {
            // Arrange
            var called = false;
            var expectedHighlight = GetHighlight(5, 5, 5, 5);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 1, MockBehavior.Strict));

            var csharpHighlight = GetHighlight(100, 100, 100, 100);
            var requestInvoker = GetRequestInvoker<DocumentHighlightParams, DocumentHighlight[]>(
                new[] { csharpHighlight },
                (method, serverContentType, highlightParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentDocumentHighlightName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                });

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = GetProjectionProvider(projectionResult);

            var documentMappingProvider = GetDocumentMappingProvider(expectedHighlight.Range, 0 /* Different from document version (1) */, RazorLanguageKind.CSharp);

            var highlightHandler = new DocumentHighlightHandler(requestInvoker, documentManager, projectionProvider, documentMappingProvider, LoggerProvider);
            var highlightRequest = new DocumentHighlightParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await highlightHandler.HandleRequestAsync(highlightRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.Empty(result);
        }

        [Fact]
        public async Task HandleRequestAsync_RemapFailure_DiscardsLocation()
        {
            // Arrange
            var called = false;
            var expectedHighlight = GetHighlight(5, 5, 5, 5);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 0, MockBehavior.Strict));

            var csharpHighlight = GetHighlight(100, 100, 100, 100);
            var requestInvoker = GetRequestInvoker<DocumentHighlightParams, DocumentHighlight[]>(
                new[] { csharpHighlight },
                (method, serverContentType, highlightParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentDocumentHighlightName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                });

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = GetProjectionProvider(projectionResult);

            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict).Object;
            Mock.Get(documentMappingProvider).Setup(p => p.MapToDocumentRangesAsync(RazorLanguageKind.CSharp, Uri, It.IsAny<Range[]>(), CancellationToken.None))
                .Returns(Task.FromResult<RazorMapToDocumentRangesResponse>(null));

            var highlightHandler = new DocumentHighlightHandler(requestInvoker, documentManager, projectionProvider, documentMappingProvider, LoggerProvider);
            var highlightRequest = new DocumentHighlightParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await highlightHandler.HandleRequestAsync(highlightRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.Empty(result);
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

        private LSPDocumentMappingProvider GetDocumentMappingProvider(Range expectedRange, int expectedVersion, RazorLanguageKind languageKind)
        {
            var remappingResult = new RazorMapToDocumentRangesResponse()
            {
                Ranges = new[] { expectedRange },
                HostDocumentVersion = expectedVersion
            };
            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(d => d.MapToDocumentRangesAsync(languageKind, Uri, It.IsAny<Range[]>(), It.IsAny<CancellationToken>())).
                Returns(Task.FromResult(remappingResult));

            return documentMappingProvider.Object;
        }

        private DocumentHighlight GetHighlight(int startLine, int startCharacter, int endLine, int endCharacter)
        {
            return new DocumentHighlight()
            {
                Range = new Range()
                {
                    Start = new Position(startLine, startCharacter),
                    End = new Position(endLine, endCharacter)
                }
            };
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public class GoToDefinitionHandlerTest
    {
        public GoToDefinitionHandlerTest()
        {
            var joinableTaskContext = new JoinableTaskContextNode(new JoinableTaskContext());
            JoinableTaskContext = joinableTaskContext.Context;
            Uri = new Uri("C:/path/to/file.razor");
        }

        private JoinableTaskContext JoinableTaskContext { get; }

        private Uri Uri { get; }

        [Fact]
        public async Task HandleRequestAsync_DocumentNotFound_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var requestInvoker = Mock.Of<LSPRequestInvoker>();
            var projectionProvider = Mock.Of<LSPProjectionProvider>();
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>();
            var definitionHandler = new GoToDefinitionHandler(JoinableTaskContext, requestInvoker, documentManager, projectionProvider, documentMappingProvider);
            var definitionRequest = new TextDocumentPositionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1)
            };

            // Act
            var result = await definitionHandler.HandleRequestAsync(definitionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

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
            var definitionHandler = new GoToDefinitionHandler(JoinableTaskContext, requestInvoker, documentManager, projectionProvider, documentMappingProvider);
            var definitionRequest = new TextDocumentPositionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1)
            };

            // Act
            var result = await definitionHandler.HandleRequestAsync(definitionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_HtmlProjection_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>());
            var requestInvoker = Mock.Of<LSPRequestInvoker>();

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>();
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>();
            var definitionHandler = new GoToDefinitionHandler(JoinableTaskContext, requestInvoker, documentManager, projectionProvider.Object, documentMappingProvider);
            var definitionRequest = new TextDocumentPositionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1)
            };

            // Act
            var result = await definitionHandler.HandleRequestAsync(definitionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_InvokesCSharpLanguageServer()
        {
            // Arrange
            var called = false;
            var expectedLocation = GetLocation(5, 5, 5, 5, Uri);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>());

            var virtualCSharpUri = new Uri("C:/path/to/file.razor.g.cs");
            var csharpLocation = GetLocation(100, 100, 100, 100, virtualCSharpUri);
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<TextDocumentPositionParams, Location[]>(It.IsAny<string>(), It.IsAny<LanguageServerKind>(), It.IsAny<TextDocumentPositionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, LanguageServerKind, TextDocumentPositionParams, CancellationToken>((method, serverKind, definitionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentDefinitionName, method);
                    Assert.Equal(LanguageServerKind.CSharp, serverKind);
                    called = true;
                })
                .Returns(Task.FromResult(new[] { csharpLocation }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var remappingResult = new RazorMapToDocumentRangeResponse()
            {
                Range = expectedLocation.Range
            };
            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(d => d.MapToDocumentRangeAsync(RazorLanguageKind.CSharp, Uri, csharpLocation.Range, It.IsAny<CancellationToken>())).
                Returns(Task.FromResult(remappingResult));

            var definitionHandler = new GoToDefinitionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, documentMappingProvider.Object);
            var definitionRequest = new TextDocumentPositionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await definitionHandler.HandleRequestAsync(definitionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            var actualLocation = Assert.Single(result);
            Assert.Equal(expectedLocation, actualLocation);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_RemapsExternalRazorFiles()
        {
            // Arrange
            var called = false;
            var externalUri = new Uri("C:/path/to/someotherfile.razor");
            var expectedLocation = GetLocation(5, 5, 5, 5, externalUri);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>());
            documentManager.AddDocument(externalUri, Mock.Of<LSPDocumentSnapshot>());

            var virtualCSharpUri = new Uri("C:/path/to/someotherfile.razor.g.cs");
            var csharpLocation = GetLocation(100, 100, 100, 100, virtualCSharpUri);
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<TextDocumentPositionParams, Location[]>(It.IsAny<string>(), It.IsAny<LanguageServerKind>(), It.IsAny<TextDocumentPositionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, LanguageServerKind, TextDocumentPositionParams, CancellationToken>((method, serverKind, definitionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentDefinitionName, method);
                    Assert.Equal(LanguageServerKind.CSharp, serverKind);
                    called = true;
                })
                .Returns(Task.FromResult(new[] { csharpLocation }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var remappingResult = new RazorMapToDocumentRangeResponse()
            {
                Range = expectedLocation.Range
            };
            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(d => d.MapToDocumentRangeAsync(RazorLanguageKind.CSharp, externalUri, csharpLocation.Range, It.IsAny<CancellationToken>())).
                Returns(Task.FromResult(remappingResult));

            var definitionHandler = new GoToDefinitionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, documentMappingProvider.Object);
            var definitionRequest = new TextDocumentPositionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await definitionHandler.HandleRequestAsync(definitionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            var actualLocation = Assert.Single(result);
            Assert.Equal(expectedLocation, actualLocation);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_DoesNotRemapNonRazorFiles()
        {
            // Arrange
            var called = false;
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>());

            var externalCSharpUri = new Uri("C:/path/to/someotherfile.cs");
            var externalCsharpLocation = GetLocation(100, 100, 100, 100, externalCSharpUri);
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<TextDocumentPositionParams, Location[]>(It.IsAny<string>(), It.IsAny<LanguageServerKind>(), It.IsAny<TextDocumentPositionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, LanguageServerKind, TextDocumentPositionParams, CancellationToken>((method, serverKind, definitionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentDefinitionName, method);
                    Assert.Equal(LanguageServerKind.CSharp, serverKind);
                    called = true;
                })
                .Returns(Task.FromResult(new[] { externalCsharpLocation }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);

            var definitionHandler = new GoToDefinitionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, documentMappingProvider.Object);
            var definitionRequest = new TextDocumentPositionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await definitionHandler.HandleRequestAsync(definitionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            var actualLocation = Assert.Single(result);
            Assert.Equal(externalCsharpLocation, actualLocation);
        }

        [Fact]
        public async Task HandleRequestAsync_VersionMismatch_DiscardsLocation()
        {
            // Arrange
            var called = false;
            var expectedLocation = GetLocation(5, 5, 5, 5, Uri);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 123));

            var virtualCSharpUri = new Uri("C:/path/to/file.razor.g.cs");
            var csharpLocation = GetLocation(100, 100, 100, 100, virtualCSharpUri);
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<TextDocumentPositionParams, Location[]>(It.IsAny<string>(), It.IsAny<LanguageServerKind>(), It.IsAny<TextDocumentPositionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, LanguageServerKind, TextDocumentPositionParams, CancellationToken>((method, serverKind, definitionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentDefinitionName, method);
                    Assert.Equal(LanguageServerKind.CSharp, serverKind);
                    called = true;
                })
                .Returns(Task.FromResult(new[] { csharpLocation }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var remappingResult = new RazorMapToDocumentRangeResponse()
            {
                Range = expectedLocation.Range,
                HostDocumentVersion = 122 // Different from document version (123)
            };
            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(d => d.MapToDocumentRangeAsync(RazorLanguageKind.CSharp, Uri, csharpLocation.Range, It.IsAny<CancellationToken>())).
                Returns(Task.FromResult(remappingResult));

            var definitionHandler = new GoToDefinitionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, documentMappingProvider.Object);
            var definitionRequest = new TextDocumentPositionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await definitionHandler.HandleRequestAsync(definitionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.Empty(result);
        }

        [Fact]
        public async Task HandleRequestAsync_RemapFailure_DiscardsLocation()
        {
            // Arrange
            var called = false;
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>());

            var virtualCSharpUri = new Uri("C:/path/to/file.razor.g.cs");
            var csharpLocation = GetLocation(100, 100, 100, 100, virtualCSharpUri);
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<TextDocumentPositionParams, Location[]>(It.IsAny<string>(), It.IsAny<LanguageServerKind>(), It.IsAny<TextDocumentPositionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, LanguageServerKind, TextDocumentPositionParams, CancellationToken>((method, serverKind, definitionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentDefinitionName, method);
                    Assert.Equal(LanguageServerKind.CSharp, serverKind);
                    called = true;
                })
                .Returns(Task.FromResult(new[] { csharpLocation }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(d => d.MapToDocumentRangeAsync(RazorLanguageKind.CSharp, Uri, csharpLocation.Range, It.IsAny<CancellationToken>())).
                Returns(Task.FromResult<RazorMapToDocumentRangeResponse>(null));

            var definitionHandler = new GoToDefinitionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, documentMappingProvider.Object);
            var definitionRequest = new TextDocumentPositionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await definitionHandler.HandleRequestAsync(definitionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.Empty(result);
        }

        private Location GetLocation(int startLine, int startCharacter, int endLine, int endCharacter, Uri uri)
        {
            var location = new Location()
            {
                Uri = uri,
                Range = new Range()
                {
                    Start = new Position(startLine, startCharacter),
                    End = new Position(endLine, endCharacter)
                }
            };

            return location;
        }
    }
}

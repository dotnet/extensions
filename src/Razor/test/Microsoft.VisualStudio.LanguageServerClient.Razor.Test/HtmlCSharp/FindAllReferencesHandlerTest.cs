// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public class FindAllReferencesHandlerTest
    {
        public FindAllReferencesHandlerTest()
        {
            Uri = new Uri("C:/path/to/file.razor");
        }

        private Uri Uri { get; }

        [Fact]
        public async Task HandleRequestAsync_DocumentNotFound_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var requestInvoker = Mock.Of<LSPRequestInvoker>();
            var projectionProvider = Mock.Of<LSPProjectionProvider>();
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>();
            var referencesHandler = new FindAllReferencesHandler(requestInvoker, documentManager, projectionProvider, documentMappingProvider);
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1)
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

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
            var referencesHandler = new FindAllReferencesHandler(requestInvoker, documentManager, projectionProvider, documentMappingProvider);
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1)
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

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
            var referencesHandler = new FindAllReferencesHandler(requestInvoker, documentManager, projectionProvider.Object, documentMappingProvider);
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1)
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_InvokesCSharpLanguageServer()
        {
            // Arrange
            var called = false;
            var expectedLocation1 = GetReferenceItem(5, 5, 5, 5, Uri);
            var expectedLocation2 = GetReferenceItem(10, 10, 10, 10, Uri);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>());

            var virtualCSharpUri = new Uri("C:/path/to/file.razor.g.cs");
            var csharpLocation1 = GetReferenceItem(100, 100, 100, 100, virtualCSharpUri);
            var csharpLocation2 = GetReferenceItem(200, 200, 200, 200, virtualCSharpUri);
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<ReferenceParams, VSReferenceItem[]>(It.IsAny<string>(), It.IsAny<LanguageServerKind>(), It.IsAny<ReferenceParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, LanguageServerKind, ReferenceParams, CancellationToken>((method, serverKind, definitionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentReferencesName, method);
                    Assert.Equal(LanguageServerKind.CSharp, serverKind);
                    called = true;
                })
                .Returns(Task.FromResult(new[] { csharpLocation1, csharpLocation2 }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var remappingResult1 = new RazorMapToDocumentRangesResponse()
            {
                Ranges = new[] { expectedLocation1.Location.Range }
            };
            var remappingResult2 = new RazorMapToDocumentRangesResponse()
            {
                Ranges = new[] { expectedLocation2.Location.Range }
            };
            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider
                .Setup(d => d.MapToDocumentRangesAsync(RazorLanguageKind.CSharp, Uri, It.IsAny<Range[]>(), It.IsAny<CancellationToken>()))
                .Returns<RazorLanguageKind, Uri, Range[], CancellationToken>((languageKind, uri, ranges, ct) => Task.FromResult(ranges[0] == csharpLocation1.Location.Range ? remappingResult1 : remappingResult2));
            var referencesHandler = new FindAllReferencesHandler(requestInvoker.Object, documentManager, projectionProvider.Object, documentMappingProvider.Object);
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.Collection(result,
                a => AssertVSReferenceItem(expectedLocation1, a),
                b => AssertVSReferenceItem(expectedLocation2, b));
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_RemapsExternalRazorFiles()
        {
            // Arrange
            var externalUri = new Uri("C:/path/to/someotherfile.razor");
            var expectedLocation = GetReferenceItem(5, 5, 5, 5, externalUri);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>());
            documentManager.AddDocument(externalUri, Mock.Of<LSPDocumentSnapshot>());

            var virtualCSharpUri = new Uri("C:/path/to/someotherfile.razor.g.cs");
            var csharpLocation = GetReferenceItem(100, 100, 100, 100, virtualCSharpUri);
            var requestInvoker = MockLSPRequestInvoker(csharpLocation);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var remappingResult = new RazorMapToDocumentRangesResponse()
            {
                Ranges = new[] { expectedLocation.Location.Range }
            };
            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(d => d.MapToDocumentRangesAsync(RazorLanguageKind.CSharp, externalUri, new[] { csharpLocation.Location.Range }, It.IsAny<CancellationToken>())).
                Returns(Task.FromResult(remappingResult));

            var referencesHandler = new FindAllReferencesHandler(requestInvoker, documentManager, projectionProvider.Object, documentMappingProvider.Object);
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            var actualLocation = Assert.Single(result);
            AssertVSReferenceItem(expectedLocation, actualLocation);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_DoesNotRemapNonRazorFiles()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>());

            var externalCSharpUri = new Uri("C:/path/to/someotherfile.cs");
            var externalCsharpLocation = GetReferenceItem(100, 100, 100, 100, externalCSharpUri);
            var requestInvoker = MockLSPRequestInvoker(externalCsharpLocation);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);

            var referencesHandler = new FindAllReferencesHandler(requestInvoker, documentManager, projectionProvider.Object, documentMappingProvider.Object);
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            var actualLocation = Assert.Single(result);
            AssertVSReferenceItem(externalCsharpLocation, actualLocation);
        }

        [Fact]
        public async Task HandleRequestAsync_VersionMismatch_DiscardsLocation()
        {
            // Arrange
            var expectedLocation = GetReferenceItem(5, 5, 5, 5, Uri);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 123));

            var virtualCSharpUri = new Uri("C:/path/to/file.razor.g.cs");
            var csharpLocation = GetReferenceItem(100, 100, 100, 100, virtualCSharpUri);
            var requestInvoker = MockLSPRequestInvoker(csharpLocation);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var remappingResult = new RazorMapToDocumentRangesResponse()
            {
                Ranges = new[] { expectedLocation.Location.Range },
                HostDocumentVersion = 122 // Different from document version (123)
            };
            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(d => d.MapToDocumentRangesAsync(RazorLanguageKind.CSharp, Uri, new[] { csharpLocation.Location.Range }, It.IsAny<CancellationToken>())).
                Returns(Task.FromResult(remappingResult));

            var referencesHandler = new FindAllReferencesHandler(requestInvoker, documentManager, projectionProvider.Object, documentMappingProvider.Object);
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task HandleRequestAsync_RemapFailure_DiscardsLocation()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>());

            var virtualCSharpUri = new Uri("C:/path/to/file.razor.g.cs");
            var csharpLocation = GetReferenceItem(100, 100, 100, 100, virtualCSharpUri);
            var requestInvoker = MockLSPRequestInvoker(csharpLocation);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(d => d.MapToDocumentRangesAsync(RazorLanguageKind.CSharp, Uri, new[] { csharpLocation.Location.Range }, It.IsAny<CancellationToken>())).
                Returns(Task.FromResult<RazorMapToDocumentRangesResponse>(null));

            var referencesHandler = new FindAllReferencesHandler(requestInvoker, documentManager, projectionProvider.Object, documentMappingProvider.Object);
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5)
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Empty(result);
        }

        private void AssertVSReferenceItem(VSReferenceItem expected, VSReferenceItem actual)
        {
            Assert.Equal(expected.Location, actual.Location);
            Assert.Equal(expected.DisplayPath, actual.DisplayPath);
        }

        private static LSPRequestInvoker MockLSPRequestInvoker(VSReferenceItem csharpLocation)
        {
            return Mock.Of<LSPRequestInvoker>(i =>
                i.ReinvokeRequestOnServerAsync<ReferenceParams, VSReferenceItem[]>(It.IsAny<string>(), It.IsAny<LanguageServerKind>(), It.IsAny<ReferenceParams>(), It.IsAny<CancellationToken>()) == Task.FromResult(new[] { csharpLocation }));
        }

        private VSReferenceItem GetReferenceItem(
            int startLine,
            int startCharacter,
            int endLine,
            int endCharacter,
            Uri uri,
            string documentName = "document",
            string projectName = "project")
        {
            return new VSReferenceItem()
            {
                Location = new Location()
                {
                    Uri = uri,
                    Range = new Range()
                    {
                        Start = new Position(startLine, startCharacter),
                        End = new Position(endLine, endCharacter)
                    }
                },
                DocumentName = documentName,
                ProjectName = projectName,
                DisplayPath = uri.AbsolutePath
            };
        }
    }
}

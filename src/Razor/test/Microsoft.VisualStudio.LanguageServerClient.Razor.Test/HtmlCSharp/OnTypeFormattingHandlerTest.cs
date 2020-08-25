// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public class OnTypeFormattingHandlerTest
    {
        public OnTypeFormattingHandlerTest()
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
            var mappingProvider = Mock.Of<LSPDocumentMappingProvider>();
            var formatOnTypeHandler = new OnTypeFormattingHandler(documentManager, requestInvoker, projectionProvider, mappingProvider);
            var formattingRequest = new DocumentOnTypeFormattingParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1),
                Character = ";",
                Options = new FormattingOptions()
            };

            // Act
            var result = await formatOnTypeHandler.HandleRequestAsync(formattingRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_ProjectionNotFound_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var snapshot = new StringTextSnapshot(@"
@code {
public string _foo;
}");
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(m => m.Snapshot == snapshot));
            var requestInvoker = Mock.Of<LSPRequestInvoker>();
            var projectionProvider = Mock.Of<LSPProjectionProvider>();
            var mappingProvider = Mock.Of<LSPDocumentMappingProvider>();
            var formatOnTypeHandler = new OnTypeFormattingHandler(documentManager, requestInvoker, projectionProvider, mappingProvider);
            var formattingRequest = new DocumentOnTypeFormattingParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1),
                Character = ";",
                Options = new FormattingOptions()
            };

            // Act
            var result = await formatOnTypeHandler.HandleRequestAsync(formattingRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_HtmlProjection_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var snapshot = new StringTextSnapshot(@"
@code {
public string _foo;
}");
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(m => m.Snapshot == snapshot));
            var requestInvoker = Mock.Of<LSPRequestInvoker>();
            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>();
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));
            var mappingProvider = Mock.Of<LSPDocumentMappingProvider>();
            var formatOnTypeHandler = new OnTypeFormattingHandler(documentManager, requestInvoker, projectionProvider.Object, mappingProvider);
            var formattingRequest = new DocumentOnTypeFormattingParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1),
                Character = ";",
                Options = new FormattingOptions()
            };

            // Act
            var result = await formatOnTypeHandler.HandleRequestAsync(formattingRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_RazorProjection_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var snapshot = new StringTextSnapshot(@"
@code {
public string _foo;
}");
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(m => m.Snapshot == snapshot));
            var requestInvoker = Mock.Of<LSPRequestInvoker>();
            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Razor,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>();
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));
            var mappingProvider = Mock.Of<LSPDocumentMappingProvider>();
            var formatOnTypeHandler = new OnTypeFormattingHandler(documentManager, requestInvoker, projectionProvider.Object, mappingProvider);
            var formattingRequest = new DocumentOnTypeFormattingParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1),
                Character = ";",
                Options = new FormattingOptions()
            };

            // Act
            var result = await formatOnTypeHandler.HandleRequestAsync(formattingRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_UnexpectedTriggerCharacter_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var snapshot = new StringTextSnapshot(@"
@code {
public string _foo;
}");
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(m => m.Snapshot == snapshot));
            var requestInvoker = Mock.Of<LSPRequestInvoker>();
            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>();
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));
            var mappingProvider = Mock.Of<LSPDocumentMappingProvider>();
            var formatOnTypeHandler = new OnTypeFormattingHandler(documentManager, requestInvoker, projectionProvider.Object, mappingProvider);
            var formattingRequest = new DocumentOnTypeFormattingParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1),
                Character = ".",
                Options = new FormattingOptions()
            };

            // Act
            var result = await formatOnTypeHandler.HandleRequestAsync(formattingRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_InvokesCSharpLanguageServer()
        {
            // Arrange
            var called = false;
            var expectedEdit = new TextEdit();
            var documentManager = new TestDocumentManager();
            var snapshot = new StringTextSnapshot(@"
@code {
public string _foo;
}");
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(m => m.Snapshot == snapshot));
            var requestInvoker = new Mock<LSPRequestInvoker>();
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<DocumentOnTypeFormattingParams, TextEdit[]>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DocumentOnTypeFormattingParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, DocumentOnTypeFormattingParams, CancellationToken>((method, serverContentType, onTypeFormattingParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentOnTypeFormattingName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                })
                .Returns(Task.FromResult(new[] { expectedEdit }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>();
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));
            var mappingProvider = Mock.Of<LSPDocumentMappingProvider>();
            var formatOnTypeHandler = new OnTypeFormattingHandler(documentManager, requestInvoker.Object, projectionProvider.Object, mappingProvider);
            var formattingRequest = new DocumentOnTypeFormattingParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(2, 19),
                Character = ";",
                Options = new FormattingOptions()
            };

            // Act
            var result = await formatOnTypeHandler.HandleRequestAsync(formattingRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
        }

        [Fact]
        public async Task HandleRequestAsync_InvokesCSharpLanguageServer_RemapsResults()
        {
            // Arrange
            var invokedCSharpServer = false;
            var remapped = false;
            var expectedEdit = new TextEdit();
            var remappedEdit = new TextEdit();
            var documentManager = new TestDocumentManager();
            var snapshot = new StringTextSnapshot(@"
@code {
public string _foo;
}");
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(m => m.Snapshot == snapshot));
            var requestInvoker = new Mock<LSPRequestInvoker>();
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<DocumentOnTypeFormattingParams, TextEdit[]>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DocumentOnTypeFormattingParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, DocumentOnTypeFormattingParams, CancellationToken>((method, serverContentType, onTypeFormattingParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentOnTypeFormattingName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    invokedCSharpServer = true;
                })
                .Returns(Task.FromResult(new[] { expectedEdit }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>();
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));
            var mappingProvider = new Mock<LSPDocumentMappingProvider>();
            mappingProvider
                .Setup(m => m.RemapFormattedTextEditsAsync(It.IsAny<Uri>(), It.IsAny<TextEdit[]>(), It.IsAny<FormattingOptions>(), It.IsAny<CancellationToken>()))
                .Callback(() => { remapped = true; })
                .Returns(Task.FromResult(new[] { remappedEdit }));

            var formatOnTypeHandler = new OnTypeFormattingHandler(documentManager, requestInvoker.Object, projectionProvider.Object, mappingProvider.Object);
            var formattingRequest = new DocumentOnTypeFormattingParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(2, 19),
                Character = ";",
                Options = new FormattingOptions()
            };

            // Act
            var result = await formatOnTypeHandler.HandleRequestAsync(formattingRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(invokedCSharpServer);
            Assert.True(remapped);
            var edit = Assert.Single(result);
            Assert.Same(remappedEdit, edit);
        }
    }
}

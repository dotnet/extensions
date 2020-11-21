// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public class OnAutoInsertHandlerTest
    {
        public OnAutoInsertHandlerTest()
        {
            Uri = new Uri("C:/path/to/file.razor");
        }

        private Uri Uri { get; }

        [Fact]
        public async Task HandleRequestAsync_UnknownTriggerCharacter_DoesNotInvokeServer()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(s => s.Uri == Uri));

            var invokedServer = false;
            var requestInvoker = new Mock<LSPRequestInvoker>();
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<DocumentOnAutoInsertParams, DocumentOnAutoInsertResponseItem>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DocumentOnAutoInsertParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, DocumentOnAutoInsertParams, CancellationToken>((method, serverContentType, formattingParams, ct) =>
                {
                    invokedServer = true;
                })
                .Returns(Task.FromResult(new DocumentOnAutoInsertResponseItem() { TextEdit = new TextEdit() }));

            var projectionProvider = Mock.Of<LSPProjectionProvider>();
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>();

            var handler = new OnAutoInsertHandler(documentManager, requestInvoker.Object, projectionProvider, documentMappingProvider);
            var request = new DocumentOnAutoInsertParams()
            {
                Character = "?",
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Options = new FormattingOptions()
                {
                    OtherOptions = new Dictionary<string, object>()
                },
                Position = new Position(0, 0)
            };

            // Act
            var response = await handler.HandleRequestAsync(request, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.False(invokedServer);
            Assert.Null(response);
        }

        [Fact]
        public async Task HandleRequestAsync_DocumentNotFound_DoesNotInvokeServer()
        {
            // Arrange
            var documentManager = new TestDocumentManager();

            var invokedServer = false;
            var requestInvoker = new Mock<LSPRequestInvoker>();
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<DocumentOnAutoInsertParams, DocumentOnAutoInsertResponseItem>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DocumentOnAutoInsertParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, DocumentOnAutoInsertParams, CancellationToken>((method, serverContentType, formattingParams, ct) =>
                {
                    invokedServer = true;
                })
                .Returns(Task.FromResult(new DocumentOnAutoInsertResponseItem() { TextEdit = new TextEdit() }));

            var projectionProvider = Mock.Of<LSPProjectionProvider>();
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>();

            var handler = new OnAutoInsertHandler(documentManager, requestInvoker.Object, projectionProvider, documentMappingProvider);
            var request = new DocumentOnAutoInsertParams()
            {
                Character = ">",
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Options = new FormattingOptions()
                {
                    OtherOptions = new Dictionary<string, object>()
                },
                Position = new Position(0, 0)
            };

            // Act
            var response = await handler.HandleRequestAsync(request, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.False(invokedServer);
            Assert.Null(response);
        }

        [Fact]
        public async Task HandleRequestAsync_RazorProjection_DoesNotInvokeServer()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(s => s.Uri == Uri));

            var invokedServer = false;
            var requestInvoker = new Mock<LSPRequestInvoker>();
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<DocumentOnAutoInsertParams, DocumentOnAutoInsertResponseItem>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DocumentOnAutoInsertParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, DocumentOnAutoInsertParams, CancellationToken>((method, serverContentType, formattingParams, ct) =>
                {
                    invokedServer = true;
                })
                .Returns(Task.FromResult(new DocumentOnAutoInsertResponseItem() { TextEdit = new TextEdit() }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Razor,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>();
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>();

            var handler = new OnAutoInsertHandler(documentManager, requestInvoker.Object, projectionProvider.Object, documentMappingProvider);
            var request = new DocumentOnAutoInsertParams()
            {
                Character = ">",
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Options = new FormattingOptions()
                {
                    OtherOptions = new Dictionary<string, object>()
                },
                Position = new Position(0, 0)
            };

            // Act
            var response = await handler.HandleRequestAsync(request, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.False(invokedServer);
            Assert.Null(response);
        }

        [Fact]
        public async Task HandleRequestAsync_InvokesHTMLServer_RemapsEdits()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(s => s.Uri == Uri && s.Snapshot == Mock.Of<ITextSnapshot>()));

            var invokedServer = false;
            var mappedTextEdits = false;
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<DocumentOnAutoInsertParams, DocumentOnAutoInsertResponseItem>(MSLSPMethods.OnAutoInsertName, It.IsAny<string>(), It.IsAny<DocumentOnAutoInsertParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, DocumentOnAutoInsertParams, CancellationToken>((method, serverContentType, formattingParams, ct) =>
                {
                    invokedServer = true;
                })
                .Returns(Task.FromResult(new DocumentOnAutoInsertResponseItem() { TextEdit = new TextEdit() { Range = new Range(), NewText = "sometext" } }));

            var projectionUri = new Uri(Uri.AbsoluteUri + "__virtual.html");
            var projectionResult = new ProjectionResult()
            {
                Uri = projectionUri,
                LanguageKind = RazorLanguageKind.Html,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>();
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider
                .Setup(d => d.RemapFormattedTextEditsAsync(projectionUri, It.IsAny<TextEdit[]>(), It.IsAny<FormattingOptions>(), /*containsSnippet*/ true, It.IsAny<CancellationToken>()))
                .Callback(() => { mappedTextEdits = true; })
                .Returns(Task.FromResult(new[] { new TextEdit() }));

            var handler = new OnAutoInsertHandler(documentManager, requestInvoker.Object, projectionProvider.Object, documentMappingProvider.Object);
            var request = new DocumentOnAutoInsertParams()
            {
                Character = "=",
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Options = new FormattingOptions()
                {
                    OtherOptions = new Dictionary<string, object>()
                },
                Position = new Position(1, 4)
            };

            // Act
            var response = await handler.HandleRequestAsync(request, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(invokedServer);
            Assert.True(mappedTextEdits);
            Assert.NotNull(response);
        }
        [Fact]
        public async Task HandleRequestAsync_InvokesCSharpServer_RemapsEdits()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(s => s.Uri == Uri && s.Snapshot == Mock.Of<ITextSnapshot>()));

            var invokedServer = false;
            var mappedTextEdits = false;
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<DocumentOnAutoInsertParams, DocumentOnAutoInsertResponseItem>(MSLSPMethods.OnAutoInsertName, It.IsAny<string>(), It.IsAny<DocumentOnAutoInsertParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, DocumentOnAutoInsertParams, CancellationToken>((method, serverContentType, formattingParams, ct) =>
                {
                    invokedServer = true;
                })
                .Returns(Task.FromResult(new DocumentOnAutoInsertResponseItem() { TextEdit = new TextEdit() { Range = new Range(), NewText = "sometext" } }));

            var projectionUri = new Uri(Uri.AbsoluteUri + "__virtual.html");
            var projectionResult = new ProjectionResult()
            {
                Uri = projectionUri,
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>();
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider
                .Setup(d => d.RemapFormattedTextEditsAsync(projectionUri, It.IsAny<TextEdit[]>(), It.IsAny<FormattingOptions>(), /*containsSnippet*/ true, It.IsAny<CancellationToken>()))
                .Callback(() => { mappedTextEdits = true; })
                .Returns(Task.FromResult(new[] { new TextEdit() }));

            var handler = new OnAutoInsertHandler(documentManager, requestInvoker.Object, projectionProvider.Object, documentMappingProvider.Object);
            var request = new DocumentOnAutoInsertParams()
            {
                Character = "/",
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Options = new FormattingOptions()
                {
                    OtherOptions = new Dictionary<string, object>()
                },
                Position = new Position(1, 4)
            };

            // Act
            var response = await handler.HandleRequestAsync(request, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(invokedServer);
            Assert.True(mappedTextEdits);
            Assert.NotNull(response);
        }

        private class TestDocumentManager : LSPDocumentManager
        {
            private readonly Dictionary<Uri, LSPDocumentSnapshot> _documents = new Dictionary<Uri, LSPDocumentSnapshot>();

            public override event EventHandler<LSPDocumentChangeEventArgs> Changed;

            public override bool TryGetDocument(Uri uri, out LSPDocumentSnapshot lspDocumentSnapshot)
            {
                return _documents.TryGetValue(uri, out lspDocumentSnapshot);
            }

            public void AddDocument(Uri uri, LSPDocumentSnapshot documentSnapshot)
            {
                _documents.Add(uri, documentSnapshot);

                Changed?.Invoke(this, null);
            }
        }
    }
}

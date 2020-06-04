// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

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
        public async Task HandleRequestAsync_UnknownTriggerCharacter_DoesNotInvokeServer()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(s => s.Uri == Uri));

            var invokedServer = false;
            var requestInvoker = new Mock<LSPRequestInvoker>();
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<DocumentOnTypeFormattingParams, TextEdit[]>(It.IsAny<string>(), It.IsAny<LanguageServerKind>(), It.IsAny<DocumentOnTypeFormattingParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, LanguageServerKind, DocumentOnTypeFormattingParams, CancellationToken>((method, serverKind, formattingParams, ct) =>
                {
                    invokedServer = true;
                })
                .Returns(Task.FromResult<TextEdit[]>(new[] { new TextEdit() }));

            var projectionProvider = Mock.Of<LSPProjectionProvider>();
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>();
            var editorService = Mock.Of<LSPEditorService>();

            var handler = new OnTypeFormattingHandler(documentManager, requestInvoker.Object, projectionProvider, documentMappingProvider, editorService);
            var request = new DocumentOnTypeFormattingParams()
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
            var edits = await handler.HandleRequestAsync(request, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.False(invokedServer);
            Assert.Empty(edits);
        }

        [Fact]
        public async Task HandleRequestAsync_DocumentNotFound_DoesNotInvokeServer()
        {
            // Arrange
            var documentManager = new TestDocumentManager();

            var invokedServer = false;
            var requestInvoker = new Mock<LSPRequestInvoker>();
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<DocumentOnTypeFormattingParams, TextEdit[]>(It.IsAny<string>(), It.IsAny<LanguageServerKind>(), It.IsAny<DocumentOnTypeFormattingParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, LanguageServerKind, DocumentOnTypeFormattingParams, CancellationToken>((method, serverKind, formattingParams, ct) =>
                {
                    invokedServer = true;
                })
                .Returns(Task.FromResult<TextEdit[]>(new[] { new TextEdit() }));

            var projectionProvider = Mock.Of<LSPProjectionProvider>();
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>();
            var editorService = Mock.Of<LSPEditorService>();

            var handler = new OnTypeFormattingHandler(documentManager, requestInvoker.Object, projectionProvider, documentMappingProvider, editorService);
            var request = new DocumentOnTypeFormattingParams()
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
            var edits = await handler.HandleRequestAsync(request, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.False(invokedServer);
            Assert.Empty(edits);
        }

        [Fact]
        public async Task HandleRequestAsync_NonHtmlProjection_DoesNotInvokeServer()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(s => s.Uri == Uri));

            var invokedServer = false;
            var requestInvoker = new Mock<LSPRequestInvoker>();
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<DocumentOnTypeFormattingParams, TextEdit[]>(It.IsAny<string>(), It.IsAny<LanguageServerKind>(), It.IsAny<DocumentOnTypeFormattingParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, LanguageServerKind, DocumentOnTypeFormattingParams, CancellationToken>((method, serverKind, formattingParams, ct) =>
                {
                    invokedServer = true;
                })
                .Returns(Task.FromResult<TextEdit[]>(new[] { new TextEdit() }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>();
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>();
            var editorService = Mock.Of<LSPEditorService>();

            var handler = new OnTypeFormattingHandler(documentManager, requestInvoker.Object, projectionProvider.Object, documentMappingProvider, editorService);
            var request = new DocumentOnTypeFormattingParams()
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
            var edits = await handler.HandleRequestAsync(request, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.False(invokedServer);
            Assert.Empty(edits);
        }

        [Fact]
        public async Task HandleRequestAsync_InvokesServerWithCorrectKey()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(s => s.Uri == Uri));

            var invokedServer = false;
            var requestInvoker = new Mock<LSPRequestInvoker>();
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<DocumentOnTypeFormattingParams, TextEdit[]>(It.IsAny<string>(), It.IsAny<LanguageServerKind>(), It.IsAny<DocumentOnTypeFormattingParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, LanguageServerKind, DocumentOnTypeFormattingParams, CancellationToken>((method, serverKind, formattingParams, ct) =>
                {
                    Assert.True(formattingParams.Options.OtherOptions.ContainsKey(LanguageServerConstants.ExpectsCursorPlaceholderKey));
                    invokedServer = true;
                })
                .Returns(Task.FromResult<TextEdit[]>(new[] { new TextEdit() }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>();
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>();
            var editorService = Mock.Of<LSPEditorService>();

            var handler = new OnTypeFormattingHandler(documentManager, requestInvoker.Object, projectionProvider.Object, documentMappingProvider, editorService);
            var request = new DocumentOnTypeFormattingParams()
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
            var edits = await handler.HandleRequestAsync(request, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(invokedServer);
            Assert.Empty(edits);
        }

        [Fact]
        public async Task HandleRequestAsync_InvokesServer_RemapsAndAppliesEdits()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(s => s.Uri == Uri && s.Snapshot == Mock.Of<ITextSnapshot>()));

            var invokedServer = false;
            var mappedTextEdits = false;
            var appliedTextEdits = false;
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<DocumentOnTypeFormattingParams, TextEdit[]>(Methods.TextDocumentOnTypeFormattingName, It.IsAny<LanguageServerKind>(), It.IsAny<DocumentOnTypeFormattingParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, LanguageServerKind, DocumentOnTypeFormattingParams, CancellationToken>((method, serverKind, formattingParams, ct) =>
                {
                    Assert.True(formattingParams.Options.OtherOptions.ContainsKey(LanguageServerConstants.ExpectsCursorPlaceholderKey));
                    invokedServer = true;
                })
                .Returns(Task.FromResult<TextEdit[]>(new[] { new TextEdit() { Range = new Range(), NewText = "sometext" } }));

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
                .Setup(d => d.RemapTextEditsAsync(projectionUri, It.IsAny<TextEdit[]>(), It.IsAny<CancellationToken>()))
                .Callback(() => { mappedTextEdits = true; })
                .Returns(Task.FromResult(Array.Empty<TextEdit>()));

            var editorService = new Mock<LSPEditorService>(MockBehavior.Strict);
            editorService.Setup(e => e.ApplyTextEditsAsync(Uri, It.IsAny<ITextSnapshot>(), It.IsAny<IEnumerable<TextEdit>>())).Callback(() => { appliedTextEdits = true; })
                .Returns(Task.CompletedTask);

            var handler = new OnTypeFormattingHandler(documentManager, requestInvoker.Object, projectionProvider.Object, documentMappingProvider.Object, editorService.Object);
            var request = new DocumentOnTypeFormattingParams()
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
            var edits = await handler.HandleRequestAsync(request, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(invokedServer);
            Assert.True(mappedTextEdits);
            Assert.True(appliedTextEdits);
            Assert.Empty(edits);
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

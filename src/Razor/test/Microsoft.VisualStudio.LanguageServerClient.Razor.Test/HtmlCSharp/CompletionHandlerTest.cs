// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public class CompletionHandlerTest
    {
        public CompletionHandlerTest()
        {
            var joinableTaskContext = new JoinableTaskContextNode(new JoinableTaskContext());
            JoinableTaskContext = joinableTaskContext.Context;
            Uri = new Uri("C:/path/to/file.razor");
            LSPDocumentSynchronizer = Mock.Of<LSPDocumentSynchronizer>(s => s.TrySynchronizeVirtualDocumentAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<VirtualDocumentSnapshot>(), It.IsAny<CancellationToken>()) == Task.FromResult(true));
            LanguageClientBroker = Mock.Of<ILanguageClientBroker>();
        }

        private JoinableTaskContext JoinableTaskContext { get; }

        private Uri Uri { get; }

        private LSPDocumentSynchronizer LSPDocumentSynchronizer { get; }

        private ILanguageClientBroker LanguageClientBroker { get; }

        [Fact]
        public async Task HandleRequestAsync_DocumentNotFound_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var requestInvoker = Mock.Of<LSPRequestInvoker>();
            var projectionProvider = Mock.Of<LSPProjectionProvider>();
            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker, documentManager, projectionProvider);
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.Invoked },
                Position = new Position(0, 1)
            };

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None);

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
            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker, documentManager, projectionProvider);
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.Invoked },
                Position = new Position(0, 1)
            };

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_HtmlProjection_InvokesHtmlLanguageServer()
        {
            // Arrange
            var called = false;
            var expectedResult = new[] { new CompletionItem() { InsertText = "Sample" } };
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.TriggerCharacter, TriggerCharacter = "<" },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>());

            var requestInvoker = new Mock<LSPRequestInvoker>();
            requestInvoker
                .Setup(r => r.RequestServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), It.IsAny<LanguageServerKind>(), It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, LanguageServerKind, CompletionParams, CancellationToken>((method, serverKind, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(LanguageServerKind.Html, serverKind);
                    called = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(expectedResult));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>();
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None);

            // Assert
            Assert.True(called);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_InvokesCSharpLanguageServer()
        {
            // Arrange
            var called = false;
            var expectedResult = new[] { new CompletionItem() { InsertText = "DateTime" } };
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.Invoked },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>());

            var requestInvoker = new Mock<LSPRequestInvoker>();
            requestInvoker
                .Setup(r => r.RequestServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), It.IsAny<LanguageServerKind>(), It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, LanguageServerKind, CompletionParams, CancellationToken>((method, serverKind, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(LanguageServerKind.CSharp, serverKind);
                    called = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(expectedResult));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>();
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None);

            // Assert
            Assert.True(called);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task HandleRequestAsync_HtmlProjection_IncompatibleTriggerCharacter_ReturnsNull()
        {
            // Arrange
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.TriggerCharacter, TriggerCharacter = "@" },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>());

            var requestInvoker = new Mock<LSPRequestInvoker>();

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>();
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_IncompatibleTriggerCharacter_ReturnsNull()
        {
            // Arrange
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.TriggerCharacter, TriggerCharacter = "<" },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>());

            var requestInvoker = new Mock<LSPRequestInvoker>();

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>();
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_IdentifierTriggerCharacter_InvokesCSharpLanguageServerNull()
        {
            // Arrange
            var called = false;
            var expectedResult = new[] { new CompletionItem() { InsertText = "DateTime" } };
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.TriggerCharacter, TriggerCharacter = "a" },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>());

            var requestInvoker = new Mock<LSPRequestInvoker>();
            requestInvoker
                .Setup(r => r.RequestServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), It.IsAny<LanguageServerKind>(), It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, LanguageServerKind, CompletionParams, CancellationToken>((method, serverKind, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(LanguageServerKind.CSharp, serverKind);
                    called = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(expectedResult));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>();
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None);

            // Assert
            Assert.True(called);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task HandleRequestAsync_HtmlProjection_IdentifierTriggerCharacter_InvokesHtmlLanguageServer()
        {
            // Arrange
            var called = false;
            var expectedResult = new[] { new CompletionItem() { InsertText = "Sample" } };
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.TriggerCharacter, TriggerCharacter = "h" },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>());

            var requestInvoker = new Mock<LSPRequestInvoker>();
            requestInvoker
                .Setup(r => r.RequestServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), It.IsAny<LanguageServerKind>(), It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, LanguageServerKind, CompletionParams, CancellationToken>((method, serverKind, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(LanguageServerKind.Html, serverKind);
                    called = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(expectedResult));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>();
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None);

            // Assert
            Assert.True(called);
            Assert.Equal(expectedResult, result);
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

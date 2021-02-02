// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public class RenameHandlerTest
    {
        public RenameHandlerTest()
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
            var renameHandler = new RenameHandler(requestInvoker, documentManager, projectionProvider, documentMappingProvider);
            var renameRequest = new RenameParams()
            {
                Position = new Position(0, 1),
                NewName = "NewName",
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
            };

            // Act
            var result = await renameHandler.HandleRequestAsync(renameRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

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
            var renameHandler = new RenameHandler(requestInvoker, documentManager, projectionProvider, documentMappingProvider);
            var renameRequest = new RenameParams()
            {
                Position = new Position(0, 1),
                NewName = "NewName",
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
            };

            // Act
            var result = await renameHandler.HandleRequestAsync(renameRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_HtmlProjection_RemapsWorkspaceEdit()
        {
            // Arrange
            var called = false;
            var expectedEdit = new WorkspaceEdit();
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(MockBehavior.Strict));

            var requestInvoker = GetRequestInvoker<RenameParams, WorkspaceEdit>(
                new WorkspaceEdit(),
                (method, serverContentType, renameParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentRenameName, method);
                    Assert.Equal(RazorLSPConstants.HtmlLSPContentTypeName, serverContentType);
                    called = true;
                });

            var projectionProvider = GetProjectionProvider(new ProjectionResult() { LanguageKind = RazorLanguageKind.Html });
            var documentMappingProvider = GetDocumentMappingProvider(expectedEdit);

            var renameHandler = new RenameHandler(requestInvoker, documentManager, projectionProvider, documentMappingProvider);
            var renameRequest = new RenameParams()
            {
                Position = new Position(0, 1),
                NewName = "NewName",
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
            };

            // Act
            var result = await renameHandler.HandleRequestAsync(renameRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.Equal(expectedEdit, result);

            // Actual remapping behavior is tested in LSPDocumentMappingProvider tests.
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_RemapsWorkspaceEdit()
        {
            // Arrange
            var called = false;
            var expectedEdit = new WorkspaceEdit();
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(MockBehavior.Strict));

            var requestInvoker = GetRequestInvoker<RenameParams, WorkspaceEdit>(
                new WorkspaceEdit(),
                (method, serverContentType, renameParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentRenameName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                });

            var projectionProvider = GetProjectionProvider(new ProjectionResult() { LanguageKind = RazorLanguageKind.CSharp });
            var documentMappingProvider = GetDocumentMappingProvider(expectedEdit);

            var renameHandler = new RenameHandler(requestInvoker, documentManager, projectionProvider, documentMappingProvider);
            var renameRequest = new RenameParams()
            {
                Position = new Position(0, 1),
                NewName = "NewName",
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
            };

            // Act
            var result = await renameHandler.HandleRequestAsync(renameRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.Equal(expectedEdit, result);

            // Actual remapping behavior is tested in LSPDocumentMappingProvider tests.
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

        private LSPDocumentMappingProvider GetDocumentMappingProvider(WorkspaceEdit expectedEdit)
        {
            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(d => d.RemapWorkspaceEditAsync(It.IsAny<WorkspaceEdit>(), It.IsAny<CancellationToken>())).
                Returns(Task.FromResult(expectedEdit));

            return documentMappingProvider.Object;
        }
    }
}

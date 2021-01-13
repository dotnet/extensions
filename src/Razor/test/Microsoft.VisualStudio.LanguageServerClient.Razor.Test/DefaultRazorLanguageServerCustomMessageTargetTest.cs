// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class DefaultRazorLanguageServerCustomMessageTargetTest
    {
        public DefaultRazorLanguageServerCustomMessageTargetTest()
        {
            JoinableTaskContext = new JoinableTaskContext();
        }

        private JoinableTaskContext JoinableTaskContext { get; }

        [Fact]
        public void UpdateCSharpBuffer_CannotLookupDocument_NoopsGracefully()
        {
            // Arrange
            LSPDocumentSnapshot document;
            var documentManager = new Mock<TrackingLSPDocumentManager>();
            documentManager.Setup(manager => manager.TryGetDocument(It.IsAny<Uri>(), out document))
                .Returns(false);
            var target = new DefaultRazorLanguageServerCustomMessageTarget(documentManager.Object);
            var request = new UpdateBufferRequest()
            {
                HostDocumentFilePath = "C:/path/to/file.razor",
            };

            // Act & Assert
            target.UpdateCSharpBuffer(request);
        }

        [Fact]
        public void UpdateCSharpBuffer_UpdatesDocument()
        {
            // Arrange
            var documentManager = new Mock<TrackingLSPDocumentManager>();
            documentManager.Setup(manager => manager.UpdateVirtualDocument<CSharpVirtualDocument>(It.IsAny<Uri>(), It.IsAny<IReadOnlyList<ITextChange>>(), 1337))
                .Verifiable();
            var target = new DefaultRazorLanguageServerCustomMessageTarget(documentManager.Object);
            var request = new UpdateBufferRequest()
            {
                HostDocumentFilePath = "C:/path/to/file.razor",
                HostDocumentVersion = 1337,
                Changes = Array.Empty<TextChange>(),
            };

            // Act
            target.UpdateCSharpBuffer(request);

            // Assert
            documentManager.VerifyAll();
        }

        [Fact]
        public async Task RazorRangeFormattingAsync_LanguageKindRazor_ReturnsEmpty()
        {
            // Arrange
            var documentManager = Mock.Of<TrackingLSPDocumentManager>();
            var requestInvoker = new Mock<LSPRequestInvoker>();
            var uIContextManager = new Mock<RazorUIContextManager>(MockBehavior.Strict);

            var target = new DefaultRazorLanguageServerCustomMessageTarget(documentManager, JoinableTaskContext, requestInvoker.Object, uIContextManager.Object);

            var request = new RazorDocumentRangeFormattingParams()
            {
                HostDocumentFilePath = "c:/Some/path/to/file.razor",
                Kind = RazorLanguageKind.Razor,
                ProjectedRange = new Range(),
                Options = new FormattingOptions()
                {
                    TabSize = 4,
                    InsertSpaces = true
                }
            };

            // Act
            var result = await target.RazorRangeFormattingAsync(request, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Edits);
        }

        [Fact]
        public async Task RazorRangeFormattingAsync_DocumentNotFound_ReturnsEmpty()
        {
            // Arrange
            var documentManager = Mock.Of<TrackingLSPDocumentManager>();
            var requestInvoker = new Mock<LSPRequestInvoker>();
            var uIContextManager = new Mock<RazorUIContextManager>(MockBehavior.Strict);

            var target = new DefaultRazorLanguageServerCustomMessageTarget(documentManager, JoinableTaskContext, requestInvoker.Object, uIContextManager.Object);

            var request = new RazorDocumentRangeFormattingParams()
            {
                HostDocumentFilePath = "c:/Some/path/to/file.razor",
                Kind = RazorLanguageKind.CSharp,
                ProjectedRange = new Range(),
                Options = new FormattingOptions()
                {
                    TabSize = 4,
                    InsertSpaces = true
                }
            };

            // Act
            var result = await target.RazorRangeFormattingAsync(request, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Edits);
        }

        [Fact]
        public async Task RazorRangeFormattingAsync_ValidRequest_InvokesLanguageServer()
        {
            // Arrange
            var filePath = "c:/Some/path/to/file.razor";
            var uri = new Uri(filePath);
            var virtualDocument = new CSharpVirtualDocumentSnapshot(new Uri($"{filePath}.g.cs"), Mock.Of<ITextSnapshot>(), 1);
            LSPDocumentSnapshot document = new TestLSPDocumentSnapshot(uri, 1, new[] { virtualDocument });
            var documentManager = new Mock<TrackingLSPDocumentManager>();
            documentManager.Setup(manager => manager.TryGetDocument(It.IsAny<Uri>(), out document))
                .Returns(true);

            var expectedEdit = new TextEdit()
            {
                NewText = "SomeEdit",
                Range = new Range() { Start = new Position(), End = new Position() }
            };
            var requestInvoker = new Mock<LSPRequestInvoker>();
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<DocumentRangeFormattingParams, TextEdit[]>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DocumentRangeFormattingParams>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new[] { expectedEdit }));

            var uIContextManager = new Mock<RazorUIContextManager>(MockBehavior.Strict);

            var target = new DefaultRazorLanguageServerCustomMessageTarget(documentManager.Object, JoinableTaskContext, requestInvoker.Object, uIContextManager.Object);

            var request = new RazorDocumentRangeFormattingParams()
            {
                HostDocumentFilePath = filePath,
                Kind = RazorLanguageKind.CSharp,
                ProjectedRange = new Range()
                {
                    Start = new Position(),
                    End = new Position()
                },
                Options = new FormattingOptions()
                {
                    TabSize = 4,
                    InsertSpaces = true
                }
            };

            // Act
            var result = await target.RazorRangeFormattingAsync(request, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            var edit = Assert.Single(result.Edits);
            Assert.Equal("SomeEdit", edit.NewText);
        }

        [Fact]
        public async Task ProvideCodeActionsAsync_CannotLookupDocument_ReturnsNullAsync()
        {
            // Arrange
            LSPDocumentSnapshot document;
            var documentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            documentManager.Setup(manager => manager.TryGetDocument(It.IsAny<Uri>(), out document))
                .Returns(false);
            var target = new DefaultRazorLanguageServerCustomMessageTarget(documentManager.Object);
            var request = new CodeActionParams()
            {
                TextDocument = new LanguageServer.Protocol.TextDocumentIdentifier()
                {
                    Uri = new Uri("C:/path/to/file.razor")
                }
            };

            // Act
            var result = await target.ProvideCodeActionsAsync(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ProvideCodeActionsAsync_CannotLookupVirtualDocument_ReturnsNullAsync()
        {
            // Arrange
            var testDocUri = new Uri("C:/path/to/file.razor");
            LSPDocumentSnapshot testDocument = new TestLSPDocumentSnapshot(testDocUri, 0);

            var documentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            documentManager.Setup(manager => manager.TryGetDocument(It.IsAny<Uri>(), out testDocument))
                .Returns(true);
            var target = new DefaultRazorLanguageServerCustomMessageTarget(documentManager.Object);
            var request = new CodeActionParams()
            {
                TextDocument = new LanguageServer.Protocol.TextDocumentIdentifier()
                {
                    Uri = new Uri("C:/path/to/file.razor")
                }
            };

            // Act
            var result = await target.ProvideCodeActionsAsync(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ProvideCodeActionsAsync_ReturnsCodeActionsAsync()
        {
            // Arrange
            var testDocUri = new Uri("C:/path/to/file.razor");
            var testVirtualDocUri = new Uri("C:/path/to/file2.razor.g");
            var testCSharpDocUri = new Uri("C:/path/to/file.razor.g.cs");

            var testVirtualDocument = new TestVirtualDocumentSnapshot(testVirtualDocUri, 0);
            var csharpVirtualDocument = new CSharpVirtualDocumentSnapshot(testCSharpDocUri, Mock.Of<ITextSnapshot>(), 0);
            LSPDocumentSnapshot testDocument = new TestLSPDocumentSnapshot(testDocUri, 0, testVirtualDocument, csharpVirtualDocument);

            var documentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            documentManager.Setup(manager => manager.TryGetDocument(It.IsAny<Uri>(), out testDocument))
                .Returns(true);

            var languageServer1Response = new[] { new VSCodeAction() { Title = "Response 1" } };
            var languageServer2Response = new[] { new VSCodeAction() { Title = "Response 2" } };
            IEnumerable<VSCodeAction[]> expectedResults = new List<VSCodeAction[]>() { languageServer1Response, languageServer2Response };
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker.Setup(invoker => invoker.ReinvokeRequestOnMultipleServersAsync<CodeActionParams, VSCodeAction[]>(
                Methods.TextDocumentCodeActionName,
                LanguageServerKind.CSharp.ToContentType(),
                It.IsAny<Func<JToken, bool>>(),
                It.IsAny<CodeActionParams>(),
                It.IsAny<CancellationToken>()
            )).Returns(Task.FromResult(expectedResults));

            var uIContextManager = new Mock<RazorUIContextManager>(MockBehavior.Strict);

            var target = new DefaultRazorLanguageServerCustomMessageTarget(documentManager.Object, JoinableTaskContext, requestInvoker.Object, uIContextManager.Object);
            var request = new CodeActionParams()
            {
                TextDocument = new LanguageServer.Protocol.TextDocumentIdentifier()
                {
                    Uri = testDocUri
                }
            };

            // Act
            var result = await target.ProvideCodeActionsAsync(request, CancellationToken.None);

            // Assert
            Assert.Collection(result,
                r => Assert.Equal(languageServer1Response[0].Title, r.Title),
                r => Assert.Equal(languageServer2Response[0].Title, r.Title));
        }

        [Fact]
        public async void ResolveCodeActionsAsync_ReturnsSingleCodeAction()
        {
            // Arrange
            var testCSharpDocUri = new Uri("C:/path/to/file.razor.g.cs");

            var requestInvoker = new Mock<LSPRequestInvoker>();
            var documentManager = new Mock<TrackingLSPDocumentManager>();
            var expectedCodeAction = new VSCodeAction()
            {
                Title = "Something",
                Data = new object()
            };
            var unexpectedCodeAction = new VSCodeAction()
            {
                Title = "Something Else",
                Data = new object()
            };
            IEnumerable<VSCodeAction> expectedResponses = new List<VSCodeAction>() { expectedCodeAction, unexpectedCodeAction };
            requestInvoker.Setup(invoker => invoker.ReinvokeRequestOnMultipleServersAsync<VSCodeAction, VSCodeAction>(
                MSLSPMethods.TextDocumentCodeActionResolveName,
                LanguageServerKind.CSharp.ToContentType(),
                It.IsAny<Func<JToken, bool>>(),
                It.IsAny<VSCodeAction>(),
                It.IsAny<CancellationToken>()
            )).Returns(Task.FromResult(expectedResponses));

            var uIContextManager = new Mock<RazorUIContextManager>(MockBehavior.Strict);

            var target = new DefaultRazorLanguageServerCustomMessageTarget(documentManager.Object, JoinableTaskContext, requestInvoker.Object, uIContextManager.Object);
            var request = new VSCodeAction()
            {
                Title = "Something",
            };

            // Act
            var result = await target.ResolveCodeActionsAsync(request, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(expectedCodeAction.Title, result.Title);
        }

        [Fact]
        public async Task ProvideSemanticTokensAsync_CannotLookupDocument_ReturnsNullAsync()
        {
            // Arrange
            LSPDocumentSnapshot document;
            var documentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            documentManager.Setup(manager => manager.TryGetDocument(It.IsAny<Uri>(), out document))
                .Returns(false);
            var target = new DefaultRazorLanguageServerCustomMessageTarget(documentManager.Object);
            var request = new SemanticTokensParams()
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = new Uri("C:/path/to/file.razor")
                }
            };

            // Act
            var result = await target.ProvideSemanticTokensAsync(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ProvideSemanticTokensAsync_CannotLookupVirtualDocument_ReturnsNullAsync()
        {
            // Arrange
            var testDocUri = new Uri("C:/path/to/file.razor");
            LSPDocumentSnapshot testDocument = new TestLSPDocumentSnapshot(testDocUri, 0);

            var documentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            documentManager.Setup(manager => manager.TryGetDocument(It.IsAny<Uri>(), out testDocument))
                .Returns(true);
            var target = new DefaultRazorLanguageServerCustomMessageTarget(documentManager.Object);
            var request = new SemanticTokensParams()
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = new Uri("C:/path/to/file.razor")
                }
            };

            // Act
            var result = await target.ProvideSemanticTokensAsync(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        [Obsolete]
        public async Task ProvideSemanticTokensAsync_ReturnsSemanticTokensAsync()
        {
            // Arrange
            var testDocUri = new Uri("C:/path/to/file.razor");
            var testVirtualDocUri = new Uri("C:/path/to/file2.razor.g");
            var testCSharpDocUri = new Uri("C:/path/to/file.razor.g.cs");

            var documentVersion = 0;
            var testVirtualDocument = new TestVirtualDocumentSnapshot(testVirtualDocUri, 0);
            var csharpVirtualDocument = new CSharpVirtualDocumentSnapshot(testCSharpDocUri, Mock.Of<ITextSnapshot>(), 0);
            LSPDocumentSnapshot testDocument = new TestLSPDocumentSnapshot(testDocUri, documentVersion, testVirtualDocument, csharpVirtualDocument);

            var documentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            documentManager.Setup(manager => manager.TryGetDocument(It.IsAny<Uri>(), out testDocument))
                .Returns(true);

            var expectedcSharpResults = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Proposals.SemanticTokens();
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker.Setup(invoker => invoker.ReinvokeRequestOnServerAsync<SemanticTokensParams, OmniSharp.Extensions.LanguageServer.Protocol.Models.Proposals.SemanticTokens>(
                LanguageServerConstants.LegacyRazorSemanticTokensEndpoint,
                LanguageServerKind.CSharp.ToContentType(),
                It.IsAny<SemanticTokensParams>(),
                It.IsAny<CancellationToken>()
            )).Returns(Task.FromResult(expectedcSharpResults));

            var uIContextManager = new Mock<RazorUIContextManager>(MockBehavior.Strict);

            var target = new DefaultRazorLanguageServerCustomMessageTarget(documentManager.Object, JoinableTaskContext, requestInvoker.Object, uIContextManager.Object);
            var request = new SemanticTokensParams()
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = testDocUri
                }
            };
            var expectedResults = new ProvideSemanticTokensResponse(expectedcSharpResults, documentVersion);

            // Act
            var result = await target.ProvideSemanticTokensAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResults, result);
        }

        [Fact]
        public async Task RazorServerReadyAsync_SetsUIContext()
        {
            // Arrange
            var testDocUri = new Uri("C:/path/to/file.razor");
            var testVirtualDocUri = new Uri("C:/path/to/file2.razor.g");
            var testCSharpDocUri = new Uri("C:/path/to/file.razor.g.cs");

            var testVirtualDocument = new TestVirtualDocumentSnapshot(testVirtualDocUri, 0);
            var csharpVirtualDocument = new CSharpVirtualDocumentSnapshot(testCSharpDocUri, Mock.Of<ITextSnapshot>(), 0);
            LSPDocumentSnapshot testDocument = new TestLSPDocumentSnapshot(testDocUri, 0, testVirtualDocument, csharpVirtualDocument);

            var documentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            documentManager.Setup(manager => manager.TryGetDocument(It.IsAny<Uri>(), out testDocument))
                .Returns(true);

            var expectedResults = new SemanticTokens { };
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker.Setup(invoker => invoker.ReinvokeRequestOnServerAsync<SemanticTokensParams, SemanticTokens>(
                LanguageServerConstants.LegacyRazorSemanticTokensEndpoint,
                LanguageServerKind.CSharp.ToContentType(),
                It.IsAny<SemanticTokensParams>(),
                It.IsAny<CancellationToken>()
            )).Returns(Task.FromResult(expectedResults));

            var uIContextManager = new Mock<RazorUIContextManager>(MockBehavior.Strict);
            uIContextManager.Setup(m => m.SetUIContextAsync(RazorLSPConstants.RazorActiveUIContextGuid, true, It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask)
                .Verifiable();

            var target = new DefaultRazorLanguageServerCustomMessageTarget(documentManager.Object, JoinableTaskContext, requestInvoker.Object, uIContextManager.Object);

            // Act
            await target.RazorServerReadyAsync(CancellationToken.None);

            // Assert
            uIContextManager.Verify();
        }
    }
}

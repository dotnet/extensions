// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public class CompletionHandlerTest
    {
        public CompletionHandlerTest()
        {
            JoinableTaskContext = new JoinableTaskContext();

            var navigatorSelector = BuildNavigatorSelector(new TextExtent());
            TextStructureNavigatorSelectorService = navigatorSelector;

            Uri = new Uri("C:/path/to/file.razor");

            CompletionRequestContextCache = new CompletionRequestContextCache();
        }

        private JoinableTaskContext JoinableTaskContext { get; }

        private ITextStructureNavigatorSelectorService TextStructureNavigatorSelectorService { get; }

        private CompletionRequestContextCache CompletionRequestContextCache { get; }

        private Uri Uri { get; }

        [Fact]
        public async Task HandleRequestAsync_DocumentNotFound_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var requestInvoker = Mock.Of<LSPRequestInvoker>(MockBehavior.Strict);
            var projectionProvider = Mock.Of<LSPProjectionProvider>(MockBehavior.Strict);
            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker, documentManager, projectionProvider, TextStructureNavigatorSelectorService, CompletionRequestContextCache);
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.Invoked },
                Position = new Position(0, 1)
            };

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_ProjectionNotFound_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0));
            var requestInvoker = Mock.Of<LSPRequestInvoker>(MockBehavior.Strict);
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict).Object;
            Mock.Get(projectionProvider).Setup(projectionProvider => projectionProvider.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), CancellationToken.None))
                .Returns(Task.FromResult<ProjectionResult>(null));
            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker, documentManager, projectionProvider, TextStructureNavigatorSelectorService, CompletionRequestContextCache);
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.Invoked },
                Position = new Position(0, 1)
            };

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_HtmlProjection_InvokesHtmlLanguageServer()
        {
            // Arrange
            var called = false;
            var expectedItem = new CompletionItem() { InsertText = "Sample" };
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new VSCompletionContext() { TriggerKind = CompletionTriggerKind.TriggerCharacter, TriggerCharacter = "<", InvokeKind = VSCompletionInvokeKind.Typing },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0));

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CompletionParams, CancellationToken>((method, serverContentType, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(RazorLSPConstants.HtmlLSPContentTypeName, serverContentType);
                    var vsCompletionContext = Assert.IsType<VSCompletionContext>(completionParams.Context);
                    Assert.Equal(VSCompletionInvokeKind.Typing, vsCompletionContext.InvokeKind);
                    called = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(new[] { expectedItem }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            var item = Assert.Single(((CompletionList)result.Value).Items);
            Assert.Equal(expectedItem.InsertText, item.InsertText);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_InvokesCSharpLanguageServer()
        {
            // Arrange
            var called = false;
            var expectedItem = new CompletionItem() { InsertText = "DateTime", Label = "DateTime" };
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new VSCompletionContext() { TriggerKind = CompletionTriggerKind.Invoked, InvokeKind = VSCompletionInvokeKind.Explicit },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0));

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CompletionParams, CancellationToken>((method, serverContentType, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    var vsCompletionContext = Assert.IsType<VSCompletionContext>(completionParams.Context);
                    Assert.Equal(VSCompletionInvokeKind.Explicit, vsCompletionContext.InvokeKind);
                    called = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(new[] { expectedItem }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            var item = ((CompletionList)result.Value).Items.First();
            Assert.Equal(expectedItem.InsertText, item.InsertText);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_DoNotReturnKeywordsWithoutAtAsync()
        {
            // Arrange
            var called = false;
            var expectedItem = new CompletionItem() { InsertText = "DateTime", Label = "DateTime" };
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.Invoked },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0));

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CompletionParams, CancellationToken>((method, serverContentType, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(new CompletionList
                {
                    Items = new[] { expectedItem }
                }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.True(result.HasValue);
            var _ = result.Value.Match<SumType<CompletionItem[], CompletionList>>(
                array => throw new NotImplementedException(),
                list =>
                {
                    Assert.Collection(list.Items,
                        item => Assert.Equal("DateTime", item.Label)
                    );

                    return list;
                });
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_DoNotPreselectAfterAt()
        {
            // Arrange
            var called = false;
            var expectedItem = new CompletionItem() { InsertText = "AccessViolationException", Label = "AccessViolationException", Preselect = true };
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext()
                {
                    TriggerKind = CompletionTriggerKind.TriggerCharacter,
                    TriggerCharacter = "@",
                },
                Position = new Position(0, 1),
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0));

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CompletionParams, CancellationToken>((method, serverContentType, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(new CompletionList
                {
                    Items = new[] { expectedItem }
                }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.True(result.HasValue);
            var _ = result.Value.Match<SumType<CompletionItem[], CompletionList>>(
                array => throw new NotImplementedException(),
                list =>
                {

                    Assert.Collection(list.Items,
                        item =>
                        {
                            Assert.Equal("AccessViolationException", item.Label);
                            Assert.False(item.Preselect, "Preselect should have been disabled.");
                        },
                        item => { }, item => { }, item => { }, item => { }, item => { }, item => { }, item => { }, item => { }, item => { }, item => { });

                    return list;
                });
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_ReturnsKeywordsFromRazor()
        {
            // Arrange
            var called = false;
            var expectedItem = new CompletionItem() { InsertText = "DateTime", Label = "DateTime" };
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.Invoked },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0));

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CompletionParams, CancellationToken>((method, serverContentType, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(new CompletionList
                {
                    Items = new[] { expectedItem }
                }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.True(result.HasValue);
            var _ = result.Value.Match<SumType<CompletionItem[], CompletionList>>(
                array => throw new NotImplementedException(),
                list =>
                {
                    Assert.Collection(list.Items,
                        item => Assert.Equal("DateTime", item.Label)
                    );

                    return list;
                });
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_ReturnsKeywordsFromCSharp_Triggered()
        {
            // Arrange
            var called = false;
            var expectedItems = new CompletionItem[] {
                 new CompletionItem() { InsertText = "DateTime", Label = "DateTime" },
                 new CompletionItem() { InsertText = "FROMCSHARP", Label = "for" },

            };

            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.TriggerCharacter, TriggerCharacter = "@" },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0));

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CompletionParams, CancellationToken>((method, serverContentType, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(expectedItems));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.True(result.HasValue);
            var _ = result.Value.Match<SumType<CompletionItem[], CompletionList>>(
                array => throw new NotImplementedException(),
                list =>
                {
                    Assert.Collection(list.Items,
                        item => Assert.Equal("DateTime", item.InsertText),
                        item =>
                        {
                            Assert.Equal("for", item.Label);
                            Assert.Equal("FROMCSHARP", item.InsertText);
                        },
                        item => Assert.Equal("foreach", item.Label),
                        item => Assert.Equal("while", item.Label),
                        item => Assert.Equal("switch", item.Label),
                        item => Assert.Equal("lock", item.Label),
                        item => Assert.Equal("case", item.Label),
                        item => Assert.Equal("if", item.Label),
                        item => Assert.Equal("try", item.Label),
                        item => Assert.Equal("do", item.Label),
                        item => Assert.Equal("using", item.Label)
                    );

                    return list;
                });
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_ReturnsKeywordsFromCSharp_Reinvoked()
        {
            // Arrange
            var called = false;
            var expectedItems = new CompletionItem[] {
                 new CompletionItem() { InsertText = "DateTime", Label = "DateTime" },
                 new CompletionItem() { InsertText = "FROMCSHARP", Label = "for" },

            };

            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.Invoked },
                Position = new Position(0, 1)
            };

            var documentSnapshot = new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0, snapshotContent: "@Da");
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, documentSnapshot);

            var wordSnapshotSpan = new SnapshotSpan(documentSnapshot.Snapshot, new Span(1, 2));
            var wordRange = new TextExtent(wordSnapshotSpan, isSignificant: true);
            var navigatorSelector = BuildNavigatorSelector(wordRange);
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CompletionParams, CancellationToken>((method, serverContentType, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(expectedItems));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, navigatorSelector, CompletionRequestContextCache);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.True(result.HasValue);
            var _ = result.Value.Match<SumType<CompletionItem[], CompletionList>>(
                array => throw new NotImplementedException(),
                list =>
                {
                    Assert.Collection(list.Items,
                        item => Assert.Equal("DateTime", item.InsertText),
                        item =>
                        {
                            Assert.Equal("for", item.Label);
                            Assert.Equal("FROMCSHARP", item.InsertText);
                        },
                        item => Assert.Equal("foreach", item.Label),
                        item => Assert.Equal("while", item.Label),
                        item => Assert.Equal("switch", item.Label),
                        item => Assert.Equal("lock", item.Label),
                        item => Assert.Equal("case", item.Label),
                        item => Assert.Equal("if", item.Label),
                        item => Assert.Equal("try", item.Label),
                        item => Assert.Equal("do", item.Label),
                        item => Assert.Equal("using", item.Label)
                    ); ;

                    return list;
                });
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_ReturnsKeywordsFromCSharp_Reinvoked_UsingStatement()
        {
            // Arrange
            var called = false;

            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.Invoked },
                Position = new Position(0, 3)
            };

            var documentSnapshot = new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0, snapshotContent: "@us");
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, documentSnapshot);

            var wordSnapshotSpan = new SnapshotSpan(documentSnapshot.Snapshot, new Span(1, 2));
            var wordRange = new TextExtent(wordSnapshotSpan, isSignificant: true);
            var navigatorSelector = BuildNavigatorSelector(wordRange);
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CompletionParams, CancellationToken>((method, serverContentType, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(Array.Empty<CompletionItem>()));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, navigatorSelector, CompletionRequestContextCache);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.True(result.HasValue);
            _ = result.Value.Match<SumType<CompletionItem[], CompletionList>>(
                array => throw new NotImplementedException(),
                list =>
                {
                    Assert.Collection(list.Items,
                        item => Assert.Equal("for", item.Label),
                        item => Assert.Equal("foreach", item.Label),
                        item => Assert.Equal("while", item.Label),
                        item => Assert.Equal("switch", item.Label),
                        item => Assert.Equal("lock", item.Label),
                        item => Assert.Equal("case", item.Label),
                        item => Assert.Equal("if", item.Label),
                        item => Assert.Equal("try", item.Label),
                        item => Assert.Equal("do", item.Label),
                        item => Assert.Equal("using", item.Label)
                    );

                    return list;
                });
        }

        [Fact]
        public async Task HandleRequestAsync_HtmlProjection_IncompatibleTriggerCharacter_ReturnsNull()
        {
            // Arrange
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.TriggerCharacter, TriggerCharacter = "~" },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0));

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

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
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.TriggerCharacter, TriggerCharacter = "&" },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0));

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_IdentifierTriggerCharacter_InvokesCSharpLanguageServerNull()
        {
            // Arrange
            var called = false;
            var expectedItem = new CompletionItem() { InsertText = "DateTime", Label = "DateTime" };
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.TriggerCharacter, TriggerCharacter = "a" },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0));

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CompletionParams, CancellationToken>((method, serverContentType, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(new[] { expectedItem }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.NotEmpty(((CompletionList)result.Value).Items);
            var item = ((CompletionList)result.Value).Items.First();
            Assert.Equal(expectedItem.InsertText, item.InsertText);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_TransitionTriggerCharacter_InvokesCSharpLanguageServerWithInvoke()
        {
            // Arrange
            var called = false;
            var expectedItem = new CompletionItem() { InsertText = "DateTime", Label = "DateTime" };
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.TriggerCharacter, TriggerCharacter = "a" },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0));

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CompletionParams, CancellationToken>((method, serverContentType, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    Assert.Equal(CompletionTriggerKind.Invoked, completionParams.Context.TriggerKind);
                    called = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(new[] { expectedItem }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.NotEmpty(((CompletionList)result.Value).Items);
            var item = ((CompletionList)result.Value).Items.First();
            Assert.Equal(expectedItem.InsertText, item.InsertText);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_RemoveAllDesignTimeHelpers()
        {
            // Arrange
            var called = false;
            var expectedItems = new CompletionItem[]
            {
                new CompletionItem() { InsertText = "BuildRenderTree", Label = "BuildRenderTree" },
                new CompletionItem() { InsertText = "DateTime", Label = "DateTime" },
                new CompletionItem() { InsertText = "__o", Label = "__o" },
                new CompletionItem() { InsertText = "__RazorDirectiveTokenHelpers__", Label = "__RazorDirectiveTokenHelpers__" },
                new CompletionItem() { InsertText = "_Imports", Label = "_Imports" },
            };

            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.TriggerCharacter, TriggerCharacter = "@" },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0));

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CompletionParams, CancellationToken>((method, serverContentType, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(expectedItems));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.True(result.HasValue);
            _ = result.Value.Match<SumType<CompletionItem[], CompletionList>>(
                array => throw new NotImplementedException(),
                list =>
                {
                    Assert.Collection(list.Items,
                        item => Assert.Equal("DateTime", item.InsertText),
                        item => Assert.Equal("for", item.Label),
                        item => Assert.Equal("foreach", item.Label),
                        item => Assert.Equal("while", item.Label),
                        item => Assert.Equal("switch", item.Label),
                        item => Assert.Equal("lock", item.Label),
                        item => Assert.Equal("case", item.Label),
                        item => Assert.Equal("if", item.Label),
                        item => Assert.Equal("try", item.Label),
                        item => Assert.Equal("do", item.Label),
                        item => Assert.Equal("using", item.Label)
                    ); ;

                    return list;
                });
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_OnlyRemoveCommonDesignTimeHelpers()
        {
            // Arrange
            var called = false;
            var expectedItems = new CompletionItem[] {
                new CompletionItem() { InsertText = "__RazorDirectiveTokenHelpers__", Label = "__RazorDirectiveTokenHelpers__" },
                new CompletionItem() { InsertText = "__o", Label = "__o" },
                new CompletionItem() { InsertText = "__x", Label = "__x" },
                new CompletionItem() { InsertText = "_Imports", Label = "_Imports" },
            };

            // Requesting completion at:
            //     @{ void M() { var __x = 1; __[||] } }
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.Invoked },
                Position = new Position(0, 29)
            };

            var documentSnapshot = new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0, snapshotContent: "@{ void M() { var __x = 1; __ } }");
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, documentSnapshot);

            var wordSnapshotSpan = new SnapshotSpan(documentSnapshot.Snapshot, new Span(27, 2));
            var wordRange = new TextExtent(wordSnapshotSpan, isSignificant: true);
            var navigatorSelector = BuildNavigatorSelector(wordRange);
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CompletionParams, CancellationToken>((method, serverContentType, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(expectedItems));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, navigatorSelector, CompletionRequestContextCache);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.True(result.HasValue);
            _ = result.Value.Match<SumType<CompletionItem[], CompletionList>>(
                array => throw new NotImplementedException(),
                list =>
                {
                    Assert.Collection(list.Items, item => Assert.Equal("__x", item.Label));
                    return list;
                });
        }

        [Fact]
        public async Task HandleRequestAsync_HtmlProjection_IdentifierTriggerCharacter_InvokesHtmlLanguageServer()
        {
            // Arrange
            var called = false;
            var expectedItem = new CompletionItem() { InsertText = "Sample" };
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext() { TriggerKind = CompletionTriggerKind.TriggerCharacter, TriggerCharacter = "h" },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0));

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CompletionParams, CancellationToken>((method, serverContentType, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(RazorLSPConstants.HtmlLSPContentTypeName, serverContentType);
                    called = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(new[] { expectedItem }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var result = await completionHandler.HandleRequestAsync(completionRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            var item = Assert.Single(((CompletionList)result.Value).Items);
            Assert.Equal(expectedItem.InsertText, item.InsertText);
        }

        [Fact]
        public void SetResolveData_RewritesData()
        {
            // Arrange
            var originalData = new object();
            var items = new[]
            {
                new CompletionItem() { InsertText = "Hello", Data = originalData }
            };
            var completionList = new CompletionList()
            {
                Items = items,
            };
            var documentManager = new TestDocumentManager();
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict).Object;
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict).Object;
            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker, documentManager, projectionProvider, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            completionHandler.SetResolveData(123, completionList);

            // Assert
            var item = Assert.Single(completionList.Items);
            var newData = Assert.IsType<CompletionResolveData>(item.Data);
            Assert.Same(originalData, newData.OriginalData);
        }

        [Fact]
        public async Task TryGetProvisionalCompletionsAsync_CSharpProjection_ReturnsFalse()
        {
            // Arrange
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext()
                {
                    TriggerKind = CompletionTriggerKind.TriggerCharacter,
                    TriggerCharacter = "."
                },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0));

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var (succeeded, result) = await completionHandler.TryGetProvisionalCompletionsAsync(completionRequest, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0), projectionResult, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.False(succeeded);
            Assert.Null(result);
        }

        [Fact]
        public async Task TryGetProvisionalCompletionsAsync_TriggerCharacterNotDot_ReturnsFalse()
        {
            // Arrange
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext()
                {
                    TriggerKind = CompletionTriggerKind.TriggerCharacter,
                    TriggerCharacter = "D"
                },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0));

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var (succeeded, result) = await completionHandler.TryGetProvisionalCompletionsAsync(completionRequest, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0), projectionResult, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.False(succeeded);
            Assert.Null(result);
        }

        [Fact]
        public async Task TryGetProvisionalCompletionsAsync_PreviousCharacterHtml_ReturnsFalse()
        {
            // Arrange
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext()
                {
                    TriggerKind = CompletionTriggerKind.TriggerCharacter,
                    TriggerCharacter = "."
                },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0));

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
                Position = new Position(1, 7)
            };
            var previousCharacterProjection = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(previousCharacterProjection));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var (succeeded, result) = await completionHandler.TryGetProvisionalCompletionsAsync(completionRequest, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0), projectionResult, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.False(succeeded);
            Assert.Null(result);
        }

        [Fact]
        public async Task TryGetProvisionalCompletionsAsync_ProjectionAtStartOfLine_ReturnsFalse()
        {
            // Arrange
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext()
                {
                    TriggerKind = CompletionTriggerKind.TriggerCharacter,
                    TriggerCharacter = "."
                },
                Position = new Position(0, 1)
            };

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0));

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
                Position = new Position(1, 0)
            };
            var previousCharacterProjection = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(previousCharacterProjection));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var (succeeded, result) = await completionHandler.TryGetProvisionalCompletionsAsync(completionRequest, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0), projectionResult, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.False(succeeded);
            Assert.Null(result);
        }

        [Fact]
        public async Task TryGetProvisionalCompletionsAsync_NullHostDocumentVersion_ReturnsFalse()
        {
            // Arrange
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext()
                {
                    TriggerKind = CompletionTriggerKind.TriggerCharacter,
                    TriggerCharacter = "."
                },
                Position = new Position(0, 1)
            };

            var virtualDocumentUri = new Uri("C:/path/to/file.razor__virtual.cs");

            var documentManager = new TestDocumentManager();

            var languageServerCalled = false;
            var expectedItem = new CompletionItem() { InsertText = "DateTime" };
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), RazorLSPConstants.CSharpContentTypeName, It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CompletionParams, CancellationToken>((method, serverContentType, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    languageServerCalled = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(new[] { expectedItem }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
                Position = new Position(1, 7)
            };
            var previousCharacterProjection = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
                Position = new Position(100, 10),
                PositionIndex = 1000,
                Uri = virtualDocumentUri,
                HostDocumentVersion = null,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(previousCharacterProjection));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var (succeeded, result) = await completionHandler.TryGetProvisionalCompletionsAsync(completionRequest, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0), projectionResult, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.False(succeeded);
            Assert.False(languageServerCalled);
            Assert.Equal(0, documentManager.UpdateVirtualDocumentCallCount);
            Assert.Null(result);
        }

        [Fact]
        public async Task TryGetProvisionalCompletionsAsync_AtCorrectProvisionalCompletionPoint_ReturnsExpectedResult()
        {
            // Arrange
            var completionRequest = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Context = new CompletionContext()
                {
                    TriggerKind = CompletionTriggerKind.TriggerCharacter,
                    TriggerCharacter = "."
                },
                Position = new Position(0, 1)
            };

            var virtualDocumentUri = new Uri("C:/path/to/file.razor__virtual.cs");

            var documentManager = new TestDocumentManager();

            var languageServerCalled = false;
            var expectedItem = new CompletionItem() { InsertText = "DateTime" };
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(It.IsAny<string>(), RazorLSPConstants.CSharpContentTypeName, It.IsAny<CompletionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CompletionParams, CancellationToken>((method, serverContentType, completionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentCompletionName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    languageServerCalled = true;
                })
                .Returns(Task.FromResult<SumType<CompletionItem[], CompletionList>?>(new[] { expectedItem }));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
                Position = new Position(1, 7)
            };
            var previousCharacterProjection = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
                Position = new Position(100, 10),
                PositionIndex = 1000,
                Uri = virtualDocumentUri,
                HostDocumentVersion = 1,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(previousCharacterProjection));

            var completionHandler = new CompletionHandler(JoinableTaskContext, requestInvoker.Object, documentManager, projectionProvider.Object, TextStructureNavigatorSelectorService, CompletionRequestContextCache);

            // Act
            var (succeeded, result) = await completionHandler.TryGetProvisionalCompletionsAsync(completionRequest, new TestLSPDocumentSnapshot(new Uri("C:/path/file.razor"), 0), projectionResult, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(succeeded);
            Assert.True(languageServerCalled);
            Assert.Equal(2, documentManager.UpdateVirtualDocumentCallCount);
            Assert.NotNull(result);
            var item = Assert.Single((CompletionItem[])result.Value);
            Assert.Equal(expectedItem.InsertText, item.InsertText);
        }

        [Fact]
        public void TriggerAppliedToProjection_Razor_ReturnsFalse()
        {
            // Arrange
            var completionHandler = new CompletionHandler(JoinableTaskContext, Mock.Of<LSPRequestInvoker>(MockBehavior.Strict), Mock.Of<LSPDocumentManager>(MockBehavior.Strict), Mock.Of<LSPProjectionProvider>(MockBehavior.Strict), TextStructureNavigatorSelectorService, CompletionRequestContextCache);
            var context = new CompletionContext();

            // Act
            var result = completionHandler.TriggerAppliesToProjection(context, RazorLanguageKind.Razor);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(" ", CompletionTriggerKind.TriggerCharacter, true)]
        [InlineData("<", CompletionTriggerKind.TriggerCharacter, true)]
        [InlineData("&", CompletionTriggerKind.TriggerCharacter, true)]
        [InlineData("\\", CompletionTriggerKind.TriggerCharacter, true)]
        [InlineData("/", CompletionTriggerKind.TriggerCharacter, true)]
        [InlineData("'", CompletionTriggerKind.TriggerCharacter, true)]
        [InlineData("=", CompletionTriggerKind.TriggerCharacter, true)]
        [InlineData(":", CompletionTriggerKind.TriggerCharacter, true)]
        [InlineData("\"", CompletionTriggerKind.TriggerCharacter, true)]
        [InlineData(".", CompletionTriggerKind.TriggerCharacter, true)]
        [InlineData(".", CompletionTriggerKind.Invoked, true)]
        [InlineData("@", CompletionTriggerKind.TriggerCharacter, true)]
        [InlineData("@", CompletionTriggerKind.Invoked, true)]
        [InlineData("a", CompletionTriggerKind.TriggerCharacter, true)] // Auto-invoked from VS platform
        [InlineData("a", CompletionTriggerKind.Invoked, true)]
        public void TriggerAppliedToProjection_Html_ReturnsExpectedResult(string character, CompletionTriggerKind kind, bool expected)
        {
            // Arrange
            var completionHandler = new CompletionHandler(JoinableTaskContext, Mock.Of<LSPRequestInvoker>(MockBehavior.Strict), Mock.Of<LSPDocumentManager>(MockBehavior.Strict), Mock.Of<LSPProjectionProvider>(MockBehavior.Strict), TextStructureNavigatorSelectorService, CompletionRequestContextCache);
            var context = new CompletionContext()
            {
                TriggerCharacter = character,
                TriggerKind = kind
            };

            // Act
            var result = completionHandler.TriggerAppliesToProjection(context, RazorLanguageKind.Html);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(".", CompletionTriggerKind.TriggerCharacter, true)]
        [InlineData("@", CompletionTriggerKind.TriggerCharacter, true)]
        [InlineData(" ", CompletionTriggerKind.TriggerCharacter, true)]
        [InlineData("&", CompletionTriggerKind.TriggerCharacter, false)]
        [InlineData("a", CompletionTriggerKind.TriggerCharacter, true)] // Auto-invoked from VS platform
        [InlineData("a", CompletionTriggerKind.Invoked, true)]
        public void TriggerAppliedToProjection_CSharp_ReturnsExpectedResult(string character, CompletionTriggerKind kind, bool expected)
        {
            // Arrange
            var completionHandler = new CompletionHandler(JoinableTaskContext, Mock.Of<LSPRequestInvoker>(MockBehavior.Strict), Mock.Of<LSPDocumentManager>(MockBehavior.Strict), Mock.Of<LSPProjectionProvider>(MockBehavior.Strict), TextStructureNavigatorSelectorService, CompletionRequestContextCache);
            var context = new CompletionContext()
            {
                TriggerCharacter = character,
                TriggerKind = kind
            };

            // Act
            var result = completionHandler.TriggerAppliesToProjection(context, RazorLanguageKind.CSharp);

            // Assert
            Assert.Equal(expected, result);
        }

        private static ITextStructureNavigatorSelectorService BuildNavigatorSelector(TextExtent wordRange)
        {
            var navigator = new Mock<ITextStructureNavigator>(MockBehavior.Strict);
            navigator.Setup(n => n.GetExtentOfWord(It.IsAny<SnapshotPoint>()))
                .Returns(wordRange);
            var navigatorSelector = new Mock<ITextStructureNavigatorSelectorService>(MockBehavior.Strict);
            navigatorSelector.Setup(selector => selector.GetTextStructureNavigator(It.IsAny<ITextBuffer>()))
                .Returns(navigator.Object);
            return navigatorSelector.Object;
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Moq;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using OmniSharp.Extensions.JsonRpc;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    public class CodeActionEndpointTest : LanguageServerTestBase
    {
        private readonly RazorDocumentMappingService DocumentMappingService = Mock.Of<RazorDocumentMappingService>(s => s.TryMapToProjectedDocumentRange(It.IsAny<RazorCodeDocument>(), It.IsAny<Range>(), out It.Ref<Range>.IsAny) == false, MockBehavior.Strict);
        private readonly DocumentResolver EmptyDocumentResolver = Mock.Of<DocumentResolver>(r => r.TryResolveDocument(It.IsAny<string>(), out It.Ref<DocumentSnapshot>.IsAny) == false, MockBehavior.Strict);
        private readonly LanguageServerFeatureOptions LanguageServerFeatureOptions = Mock.Of<LanguageServerFeatureOptions>(l => l.SupportsFileManipulation == true, MockBehavior.Strict);
        private readonly ClientNotifierServiceBase LanguageServer = Mock.Of<ClientNotifierServiceBase>(MockBehavior.Strict);

        [Fact]
        public async Task Handle_NoDocument()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeActionEndpoint = new CodeActionEndpoint(
                DocumentMappingService,
                Array.Empty<RazorCodeActionProvider>(),
                Array.Empty<CSharpCodeActionProvider>(),
                Dispatcher,
                EmptyDocumentResolver,
                LanguageServer,
                LanguageServerFeatureOptions)
            {
                _supportsCodeActionResolve = false
            };
            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 1), new Position(0, 1)),
                Context = new ExtendedCodeActionContext()
            };

            // Act
            var commandOrCodeActionContainer = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.Null(commandOrCodeActionContainer);
        }

        [Fact]
        public async Task Handle_UnsupportedDocument()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            codeDocument.SetUnsupported();
            var codeActionEndpoint = new CodeActionEndpoint(
                DocumentMappingService,
                Array.Empty<RazorCodeActionProvider>(),
                Array.Empty<CSharpCodeActionProvider>(),
                Dispatcher,
                documentResolver,
                LanguageServer,
                LanguageServerFeatureOptions)
            {
                _supportsCodeActionResolve = false
            };
            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 1), new Position(0, 1)),
                Context = new ExtendedCodeActionContext()
            };

            // Act
            var commandOrCodeActionContainer = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.Null(commandOrCodeActionContainer);
        }

        [Fact]
        public async Task Handle_NoProviders()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var codeActionEndpoint = new CodeActionEndpoint(
                DocumentMappingService,
                Array.Empty<RazorCodeActionProvider>(),
                Array.Empty<CSharpCodeActionProvider>(),
                Dispatcher,
                documentResolver,
                LanguageServer,
                LanguageServerFeatureOptions)
            {
                _supportsCodeActionResolve = false
            };
            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 1), new Position(0, 1)),
                Context = new ExtendedCodeActionContext()
            };

            // Act
            var commandOrCodeActionContainer = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.Null(commandOrCodeActionContainer);
        }

        [Fact]
        public async Task Handle_OneRazorCodeActionProvider()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var codeActionEndpoint = new CodeActionEndpoint(
                DocumentMappingService,
                new RazorCodeActionProvider[] {
                    new MockRazorCodeActionProvider()
                },
                Array.Empty<CSharpCodeActionProvider>(),
                Dispatcher,
                documentResolver,
                LanguageServer,
                LanguageServerFeatureOptions)
            {
                _supportsCodeActionResolve = false
            };

            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 1), new Position(0, 1)),
                Context = new ExtendedCodeActionContext()
            };

            // Act
            var commandOrCodeActionContainer = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.Single(commandOrCodeActionContainer);
        }

        [Fact]
        public async Task Handle_OneCSharpCodeActionProvider()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var documentMappingService = CreateDocumentMappingService();
            var languageServer = CreateLanguageServer();
            var codeActionEndpoint = new CodeActionEndpoint(
                documentMappingService,
                Array.Empty<RazorCodeActionProvider>(),
                new CSharpCodeActionProvider[] {
                    new MockCSharpCodeActionProvider()
                },
                Dispatcher,
                documentResolver,
                languageServer,
                LanguageServerFeatureOptions)
            {
                _supportsCodeActionResolve = false
            };

            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 1), new Position(0, 1)),
                Context = new ExtendedCodeActionContext()
            };

            // Act
            var commandOrCodeActionContainer = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.Single(commandOrCodeActionContainer);
        }

        [Fact]
        public async Task Handle_OneCodeActionProviderWithMultipleCodeActions()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var codeActionEndpoint = new CodeActionEndpoint(
                DocumentMappingService,
                new RazorCodeActionProvider[] {
                    new MockMultipleRazorCodeActionProvider(),
                },
                Array.Empty<CSharpCodeActionProvider>(),
                Dispatcher,
                documentResolver,
                LanguageServer,
                LanguageServerFeatureOptions)
            {
                _supportsCodeActionResolve = false
            };

            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 1), new Position(0, 1)),
                Context = new ExtendedCodeActionContext()
            };

            // Act
            var commandOrCodeActionContainer = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.Equal(2, commandOrCodeActionContainer.Count());
        }

        [Fact]
        public async Task Handle_MultipleCodeActionProvidersWithMultipleCodeActions()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var documentMappingService = CreateDocumentMappingService();
            var languageServer = CreateLanguageServer();
            var codeActionEndpoint = new CodeActionEndpoint(
                documentMappingService,
                new RazorCodeActionProvider[] {
                    new MockMultipleRazorCodeActionProvider(),
                    new MockMultipleRazorCodeActionProvider(),
                    new MockRazorCodeActionProvider(),
                },
                new CSharpCodeActionProvider[] {
                    new MockCSharpCodeActionProvider(),
                    new MockCSharpCodeActionProvider()
                },
                Dispatcher,
                documentResolver,
                languageServer,
                LanguageServerFeatureOptions)
            {
                _supportsCodeActionResolve = false
            };

            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 1), new Position(0, 1)),
                Context = new ExtendedCodeActionContext()
            };

            // Act
            var commandOrCodeActionContainer = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.Equal(7, commandOrCodeActionContainer.Count());
        }

        [Fact]
        public async Task Handle_MultipleProviders()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var documentMappingService = CreateDocumentMappingService();
            var languageServer = CreateLanguageServer();
            var codeActionEndpoint = new CodeActionEndpoint(
                documentMappingService,
                new RazorCodeActionProvider[] {
                    new MockRazorCodeActionProvider(),
                    new MockRazorCodeActionProvider(),
                    new MockRazorCodeActionProvider(),
                },
                new CSharpCodeActionProvider[] {
                    new MockCSharpCodeActionProvider(),
                    new MockCSharpCodeActionProvider()
                },
                Dispatcher,
                documentResolver,
                languageServer,
                LanguageServerFeatureOptions)
            {
                _supportsCodeActionResolve = false
            };

            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 1), new Position(0, 1)),
                Context = new ExtendedCodeActionContext()
            };

            // Act
            var commandOrCodeActionContainer = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.Equal(5, commandOrCodeActionContainer.Count());
        }

        [Fact]
        public async Task Handle_OneNullReturningProvider()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var codeActionEndpoint = new CodeActionEndpoint(
                DocumentMappingService,
                new RazorCodeActionProvider[] {
                    new MockNullRazorCodeActionProvider()
                },
                Array.Empty<CSharpCodeActionProvider>(),
                Dispatcher,
                documentResolver,
                LanguageServer,
                LanguageServerFeatureOptions)
            {
                _supportsCodeActionResolve = false
            };

            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 1), new Position(0, 1)),
                Context = new ExtendedCodeActionContext()
            };

            // Act
            var commandOrCodeActionContainer = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.Null(commandOrCodeActionContainer);
        }

        [Fact]
        public async Task Handle_MultipleMixedProvider()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var documentMappingService = CreateDocumentMappingService();
            var languageServer = CreateLanguageServer();
            var codeActionEndpoint = new CodeActionEndpoint(
                documentMappingService,
                new RazorCodeActionProvider[] {
                    new MockRazorCodeActionProvider(),
                    new MockNullRazorCodeActionProvider(),
                    new MockRazorCodeActionProvider(),
                    new MockNullRazorCodeActionProvider(),
                },
                new CSharpCodeActionProvider[] {
                    new MockCSharpCodeActionProvider(),
                    new MockCSharpCodeActionProvider()
                },
                Dispatcher,
                documentResolver,
                languageServer,
                LanguageServerFeatureOptions)
            {
                _supportsCodeActionResolve = false
            };

            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 1), new Position(0, 1)),
                Context = new ExtendedCodeActionContext()
            };

            // Act
            var commandOrCodeActionContainer = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.Equal(4, commandOrCodeActionContainer.Count());
        }

        [Fact]
        public async Task Handle_MultipleMixedProvider_SupportsCodeActionResolveTrue()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var codeActionEndpoint = new CodeActionEndpoint(
                DocumentMappingService,
                new RazorCodeActionProvider[] {
                    new MockRazorCodeActionProvider(),
                    new MockRazorCommandProvider(),
                    new MockNullRazorCodeActionProvider()
                },
                Array.Empty<CSharpCodeActionProvider>(),
                Dispatcher,
                documentResolver,
                LanguageServer,
                LanguageServerFeatureOptions)
            {
                _supportsCodeActionResolve = true
            };

            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 1), new Position(0, 1)),
                Context = new ExtendedCodeActionContext()
            };

            // Act
            var commandOrCodeActionContainer = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.Collection(commandOrCodeActionContainer,
                c =>
                {
                    Assert.True(c.IsCodeAction);
                    Assert.True(c.CodeAction is RazorCodeAction);
                },
                c =>
                {
                    Assert.True(c.IsCodeAction);
                    Assert.True(c.CodeAction is RazorCodeAction);
                });
        }

        [Fact]
        public async Task Handle_MultipleMixedProvider_SupportsCodeActionResolveFalse()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var codeActionEndpoint = new CodeActionEndpoint(
                DocumentMappingService,
                new RazorCodeActionProvider[] {
                    new MockRazorCodeActionProvider(),
                    new MockRazorCommandProvider(),
                    new MockNullRazorCodeActionProvider()
                },
                Array.Empty<CSharpCodeActionProvider>(),
                Dispatcher,
                documentResolver,
                LanguageServer,
                LanguageServerFeatureOptions)
            {
                _supportsCodeActionResolve = false
            };

            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 1), new Position(0, 1)),
                Context = new ExtendedCodeActionContext()
            };

            // Act
            var commandOrCodeActionContainer = await codeActionEndpoint.Handle(request, default);

            // Assert
            Assert.Collection(commandOrCodeActionContainer,
                c =>
                {
                    Assert.True(c.IsCommand);
                    var command = Assert.IsType<Command>(c.Command);
                    var codeActionParams = command.Arguments.First().ToObject<RazorCodeActionResolutionParams>();
                    Assert.Equal(LanguageServerConstants.CodeActions.EditBasedCodeActionCommand, codeActionParams.Action);
                },
                c => Assert.True(c.IsCommand));
        }

        [Fact]
        public async Task GenerateRazorCodeActionContextAsync_WithSelectionRange()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var codeActionEndpoint = new CodeActionEndpoint(
                DocumentMappingService,
                new RazorCodeActionProvider[] {
                    new MockRazorCodeActionProvider()
                },
                Array.Empty<CSharpCodeActionProvider>(),
                Dispatcher,
                documentResolver,
                LanguageServer,
                LanguageServerFeatureOptions)
            {
                _supportsCodeActionResolve = false
            };

            var initialRange = new Range(new Position(0, 1), new Position(0, 1));
            var selectionRange = new Range(new Position(0, 5), new Position(0, 5));
            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = initialRange,
                Context = new ExtendedCodeActionContext()
                {
                    SelectionRange = selectionRange,
                }
            };

            // Act
            var razorCodeActionContext = await codeActionEndpoint.GenerateRazorCodeActionContextAsync(request, default);

            // Assert
            Assert.NotNull(razorCodeActionContext);
            Assert.Equal(selectionRange, razorCodeActionContext.Request.Range);
        }

        [Fact]
        public async Task GenerateRazorCodeActionContextAsync_WithoutSelectionRange()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var codeActionEndpoint = new CodeActionEndpoint(
                DocumentMappingService,
                new RazorCodeActionProvider[] {
                    new MockRazorCodeActionProvider()
                },
                Array.Empty<CSharpCodeActionProvider>(),
                Dispatcher,
                documentResolver,
                LanguageServer,
                LanguageServerFeatureOptions)
            {
                _supportsCodeActionResolve = false
            };

            var initialRange = new Range(new Position(0, 1), new Position(0, 1));
            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = initialRange,
                Context = new ExtendedCodeActionContext()
                {
                    SelectionRange = null
                }
            };

            // Act
            var razorCodeActionContext = await codeActionEndpoint.GenerateRazorCodeActionContextAsync(request, default);

            // Assert
            Assert.NotNull(razorCodeActionContext);
            Assert.Equal(initialRange, razorCodeActionContext.Request.Range);
        }

        [Fact]
        public async Task GetCSharpCodeActionsFromLanguageServerAsync_InvalidRangeMapping()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            Range projectedRange = null;
            var documentMappingService = Mock.Of<DefaultRazorDocumentMappingService>(
                d => d.TryMapToProjectedDocumentRange(It.IsAny<RazorCodeDocument>(), It.IsAny<Range>(), out projectedRange) == false
            , MockBehavior.Strict);
            var codeActionEndpoint = new CodeActionEndpoint(
                documentMappingService,
                Array.Empty<RazorCodeActionProvider>(),
                new CSharpCodeActionProvider[] {
                    new MockCSharpCodeActionProvider()
                },
                Dispatcher,
                documentResolver,
                LanguageServer,
                LanguageServerFeatureOptions)
            {
                _supportsCodeActionResolve = false
            };

            var initialRange = new Range(new Position(0, 1), new Position(0, 1));
            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = initialRange,
                Context = new ExtendedCodeActionContext()
            };

            var context = await codeActionEndpoint.GenerateRazorCodeActionContextAsync(request, default);

            // Act
            var results = await codeActionEndpoint.GetCSharpCodeActionsFromLanguageServerAsync(context, default);

            // Assert
            Assert.Empty(results);
            Assert.Equal(initialRange, context.Request.Range);
        }

        [Fact]
        public async Task GetCSharpCodeActionsFromLanguageServerAsync_ReturnsCodeActions()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var projectedRange = new Range(new Position(15, 2), new Position(15, 2));
            var documentMappingService = CreateDocumentMappingService(projectedRange);
            var languageServer = CreateLanguageServer();
            var codeActionEndpoint = new CodeActionEndpoint(
                documentMappingService,
                Array.Empty<RazorCodeActionProvider>(),
                new CSharpCodeActionProvider[] {
                    new MockCSharpCodeActionProvider()
                },
                Dispatcher,
                documentResolver,
                languageServer,
                LanguageServerFeatureOptions)
            {
                _supportsCodeActionResolve = false
            };

            var initialRange = new Range(new Position(0, 1), new Position(0, 1));
            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = initialRange,
                Context = new ExtendedCodeActionContext()
            };

            var context = await codeActionEndpoint.GenerateRazorCodeActionContextAsync(request, default);

            // Act
            var results = await codeActionEndpoint.GetCSharpCodeActionsFromLanguageServerAsync(context, default);

            // Assert
            Assert.Single(results);
            Assert.Equal(projectedRange, context.Request.Range);
        }

        private static DefaultRazorDocumentMappingService CreateDocumentMappingService(Range projectedRange = null)
        {
            projectedRange ??= new Range(new Position(5, 2), new Position(5, 2));
            var documentMappingService = Mock.Of<DefaultRazorDocumentMappingService>(
                d => d.TryMapToProjectedDocumentRange(It.IsAny<RazorCodeDocument>(), It.IsAny<Range>(), out projectedRange) == true
            , MockBehavior.Strict);
            return documentMappingService;
        }

        private static ClientNotifierServiceBase CreateLanguageServer()
        {
            var response = Mock.Of<IResponseRouterReturns>(
                r => r.Returning<RazorCodeAction[]>(It.IsAny<CancellationToken>()) == Task.FromResult(new[]
                {
                    new RazorCodeAction() { Data = JToken.FromObject(new { CustomTags = new[] { "CodeActionName" } }) }
                })
            , MockBehavior.Strict);
            var languageServer = Mock.Of<ClientNotifierServiceBase>(
                l => l.SendRequestAsync(LanguageServerConstants.RazorProvideCodeActionsEndpoint, It.IsAny<CodeActionParams>()) == Task.FromResult(response)
            , MockBehavior.Strict);
            return languageServer;
        }

        private static DocumentResolver CreateDocumentResolver(string documentPath, RazorCodeDocument codeDocument)
        {
            var sourceTextChars = new char[codeDocument.Source.Length];
            codeDocument.Source.CopyTo(0, sourceTextChars, 0, codeDocument.Source.Length);
            var sourceText = SourceText.From(new string(sourceTextChars));
            var documentSnapshot = Mock.Of<DocumentSnapshot>(document =>
                document.GetGeneratedOutputAsync() == Task.FromResult(codeDocument) &&
                document.GetTextAsync() == Task.FromResult(sourceText), MockBehavior.Strict);
            var documentResolver = new Mock<DocumentResolver>(MockBehavior.Strict);
            documentResolver
                .Setup(resolver => resolver.TryResolveDocument(documentPath, out documentSnapshot))
                .Returns(true);
            return documentResolver.Object;
        }

        private static RazorCodeDocument CreateCodeDocument(string text)
        {
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var sourceDocument = TestRazorSourceDocument.Create(text);
            var syntaxTree = RazorSyntaxTree.Parse(sourceDocument);
            codeDocument.SetSyntaxTree(syntaxTree);
            return codeDocument;
        }

        private class MockRazorCodeActionProvider : RazorCodeActionProvider
        {
            public override Task<IReadOnlyList<RazorCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
            {
                return Task.FromResult(new List<RazorCodeAction>() { new RazorCodeAction() } as IReadOnlyList<RazorCodeAction>);
            }
        }

        private class MockMultipleRazorCodeActionProvider : RazorCodeActionProvider
        {
            public override Task<IReadOnlyList<RazorCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
            {
                return Task.FromResult(new List<RazorCodeAction>()
                {
                    new RazorCodeAction(),
                    new RazorCodeAction()
                } as IReadOnlyList<RazorCodeAction>);
            }
        }

        private class MockCSharpCodeActionProvider : CSharpCodeActionProvider
        {
            public override Task<IReadOnlyList<RazorCodeAction>> ProvideAsync(RazorCodeActionContext context, IEnumerable<RazorCodeAction> codeActions, CancellationToken cancellationToken)
            {
                return Task.FromResult(new List<RazorCodeAction>()
                {
                    new RazorCodeAction()
                } as IReadOnlyList<RazorCodeAction>);
            }
        }

        private class MockRazorCommandProvider : RazorCodeActionProvider
        {
            public override Task<IReadOnlyList<RazorCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
            {
                // O# Code Actions don't have `Data`, but `Commands` do
                return Task.FromResult(new List<RazorCodeAction>() {
                    new RazorCodeAction() {
                        Title = "SomeTitle",
                        Data = JToken.FromObject(new AddUsingsCodeActionParams())
                    }
                } as IReadOnlyList<RazorCodeAction>);
            }
        }

        private class MockNullRazorCodeActionProvider : RazorCodeActionProvider
        {
            public override Task<IReadOnlyList<RazorCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
            {
                return Task.FromResult<IReadOnlyList<RazorCodeAction>>(null);
            }
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Moq;
using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.CodeActions
{
    public class CodeActionEndpointTest : LanguageServerTestBase
    {
        private readonly RazorDocumentMappingService DocumentMappingService = Mock.Of<RazorDocumentMappingService>();
        private readonly DocumentResolver EmptyDocumentResolver = Mock.Of<DocumentResolver>();
        private readonly LanguageServerFeatureOptions LanguageServerFeatureOptions = Mock.Of<LanguageServerFeatureOptions>(l => l.SupportsFileManipulation == true);
        private readonly IClientLanguageServer LanguageServer = Mock.Of<IClientLanguageServer>();

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
        public async Task Handle_OneProvider()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var codeActionEndpoint = new CodeActionEndpoint(
                DocumentMappingService,
                new RazorCodeActionProvider[] {
                    new MockCodeActionProvider()
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
        public async Task Handle_MultipleProviders()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var codeActionEndpoint = new CodeActionEndpoint(
                DocumentMappingService,
                new RazorCodeActionProvider[] {
                    new MockCodeActionProvider(),
                    new MockCodeActionProvider(),
                    new MockCodeActionProvider(),
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
            Assert.Equal(3, commandOrCodeActionContainer.Count());
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
                    new MockNullCodeActionProvider()
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
            var codeActionEndpoint = new CodeActionEndpoint(
                DocumentMappingService,
                new RazorCodeActionProvider[] {
                    new MockCodeActionProvider(),
                    new MockNullCodeActionProvider(),
                    new MockCodeActionProvider(),
                    new MockNullCodeActionProvider(),
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
        public async Task Handle_MultipleMixedProvider_SupportsCodeActionResolveTrue()
        {
            // Arrange
            var documentPath = "C:/path/to/Page.razor";
            var codeDocument = CreateCodeDocument("@code {}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var codeActionEndpoint = new CodeActionEndpoint(
                DocumentMappingService,
                new RazorCodeActionProvider[] {
                    new MockCodeActionProvider(),
                    new MockCommandProvider(),
                    new MockNullCodeActionProvider()
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
                    new MockCodeActionProvider(),
                    new MockCommandProvider(),
                    new MockNullCodeActionProvider()
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
                    Assert.True(c.IsCodeAction);
                    Assert.True(c.CodeAction is RazorCodeAction);
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
                    new MockCodeActionProvider()
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
                    new MockCodeActionProvider()
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

        private class MockCodeActionProvider : RazorCodeActionProvider
        {
            public override Task<IReadOnlyList<RazorCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
            {
                return Task.FromResult(new List<RazorCodeAction>() { new RazorCodeAction() } as IReadOnlyList<RazorCodeAction>);
            }
        }

        private class MockCommandProvider : RazorCodeActionProvider
        {
            public override Task<IReadOnlyList<RazorCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
            {
                // O# Code Actions don't have `Data`, but `Commands` do
                return Task.FromResult(new List<RazorCodeAction>() {
                    new RazorCodeAction() {
                        Title = "SomeTitle",
                        Data = new AddUsingsCodeActionParams()
                    }
                } as IReadOnlyList<RazorCodeAction>);
            }
        }

        private class MockNullCodeActionProvider : RazorCodeActionProvider
        {
            public override Task<IReadOnlyList<RazorCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
            {
                return null;
            }
        }

        private static DocumentResolver CreateDocumentResolver(string documentPath, RazorCodeDocument codeDocument)
        {
            var sourceTextChars = new char[codeDocument.Source.Length];
            codeDocument.Source.CopyTo(0, sourceTextChars, 0, codeDocument.Source.Length);
            var sourceText = SourceText.From(new string(sourceTextChars));
            var documentSnapshot = Mock.Of<DocumentSnapshot>(document =>
                document.GetGeneratedOutputAsync() == Task.FromResult(codeDocument) &&
                document.GetTextAsync() == Task.FromResult(sourceText));
            var documentResolver = new Mock<DocumentResolver>();
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
    }
}

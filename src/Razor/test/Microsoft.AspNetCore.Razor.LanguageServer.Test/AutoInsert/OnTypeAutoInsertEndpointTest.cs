// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class OnTypeAutoInsertEndpointTest : LanguageServerTestBase
    {
        public OnTypeAutoInsertEndpointTest()
        {
            EmptyDocumentResolver = Mock.Of<DocumentResolver>();
        }

        private DocumentResolver EmptyDocumentResolver { get; }

        [Fact]
        public async Task Handle_SingleProvider_InvokesProvider()
        {
            // Arrange
            var codeDocument = CreateCodeDocument();
            var uri = new Uri("file://path/test.razor");
            var documentResolver = CreateDocumentResolver(uri.GetAbsoluteOrUNCPath(), codeDocument);
            var insertProvider = new TestOnAutoInsertProvider(">", canResolve: true);
            var endpoint = new OnAutoInsertEndpoint(Dispatcher, documentResolver, new[] { insertProvider });
            var @params = new OnAutoInsertParams()
            {
                TextDocument = new TextDocumentIdentifier(uri),
                Character = ">",
                Options = new FormattingOptions(),
            };

            // Act
            var result = await endpoint.Handle(@params, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(insertProvider.Called);
        }

        [Fact]
        public async Task Handle_MultipleProviderSameTrigger_UsesSuccessful()
        {
            // Arrange
            var codeDocument = CreateCodeDocument();
            var uri = new Uri("file://path/test.razor");
            var documentResolver = CreateDocumentResolver(uri.GetAbsoluteOrUNCPath(), codeDocument);
            var insertProvider1 = new TestOnAutoInsertProvider(">", canResolve: false)
            {
                ResolvedTextEdit = new TextEdit()
            };
            var insertProvider2 = new TestOnAutoInsertProvider(">", canResolve: true)
            {
                ResolvedTextEdit = new TextEdit()
            };
            var endpoint = new OnAutoInsertEndpoint(Dispatcher, documentResolver, new[] { insertProvider1, insertProvider2 });
            var @params = new OnAutoInsertParams()
            {
                TextDocument = new TextDocumentIdentifier(uri),
                Character = ">",
                Options = new FormattingOptions(),
            };

            // Act
            var result = await endpoint.Handle(@params, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(insertProvider1.Called);
            Assert.True(insertProvider2.Called);
            Assert.Same(insertProvider2.ResolvedTextEdit, result.TextEdit);
        }

        [Fact]
        public async Task Handle_MultipleProviderSameTrigger_UsesFirstSuccessful()
        {
            // Arrange
            var codeDocument = CreateCodeDocument();
            var uri = new Uri("file://path/test.razor");
            var documentResolver = CreateDocumentResolver(uri.GetAbsoluteOrUNCPath(), codeDocument);
            var insertProvider1 = new TestOnAutoInsertProvider(">", canResolve: true)
            {
                ResolvedTextEdit = new TextEdit()
            };
            var insertProvider2 = new TestOnAutoInsertProvider(">", canResolve: true)
            {
                ResolvedTextEdit = new TextEdit()
            };
            var endpoint = new OnAutoInsertEndpoint(Dispatcher, documentResolver, new[] { insertProvider1, insertProvider2 });
            var @params = new OnAutoInsertParams()
            {
                TextDocument = new TextDocumentIdentifier(uri),
                Character = ">",
                Options = new FormattingOptions(),
            };

            // Act
            var result = await endpoint.Handle(@params, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(insertProvider1.Called);
            Assert.False(insertProvider2.Called);
            Assert.Same(insertProvider1.ResolvedTextEdit, result.TextEdit);
        }

        [Fact]
        public async Task Handle_MultipleProviderUnmatchingTrigger_ReturnsNull()
        {
            // Arrange
            var codeDocument = CreateCodeDocument();
            var uri = new Uri("file://path/test.razor");
            var documentResolver = CreateDocumentResolver(uri.GetAbsoluteOrUNCPath(), codeDocument);
            var insertProvider1 = new TestOnAutoInsertProvider(">", canResolve: true);
            var insertProvider2 = new TestOnAutoInsertProvider("<", canResolve: true);
            var endpoint = new OnAutoInsertEndpoint(Dispatcher, documentResolver, new[] { insertProvider1, insertProvider2 });
            var @params = new OnAutoInsertParams()
            {
                TextDocument = new TextDocumentIdentifier(uri),
                Character = "!",
                Options = new FormattingOptions(),
            };

            // Act
            var result = await endpoint.Handle(@params, CancellationToken.None);

            // Assert
            Assert.Null(result);
            Assert.False(insertProvider1.Called);
            Assert.False(insertProvider2.Called);
        }

        [Fact]
        public async Task Handle_DocumentNotFound_ReturnsNull()
        {
            // Arrange
            var insertProvider = new TestOnAutoInsertProvider(">", canResolve: true);
            var endpoint = new OnAutoInsertEndpoint(Dispatcher, EmptyDocumentResolver, new[] { insertProvider });
            var uri = new Uri("file://path/test.razor");
            var @params = new OnAutoInsertParams()
            {
                TextDocument = new TextDocumentIdentifier(uri),
                Character = ">",
                Options = new FormattingOptions(),
            };

            // Act
            var result = await endpoint.Handle(@params, CancellationToken.None);

            // Assert
            Assert.Null(result);
            Assert.False(insertProvider.Called);
        }

        [Fact]
        public async Task Handle_UnsupportedCodeDocument_ReturnsNull()
        {
            // Arrange
            var codeDocument = CreateCodeDocument();
            codeDocument.SetUnsupported();
            var uri = new Uri("file://path/test.razor");
            var documentResolver = CreateDocumentResolver(uri.GetAbsoluteOrUNCPath(), codeDocument);
            var insertProvider = new TestOnAutoInsertProvider(">", canResolve: true);
            var endpoint = new OnAutoInsertEndpoint(Dispatcher, documentResolver, new[] { insertProvider });
            var @params = new OnAutoInsertParams()
            {
                TextDocument = new TextDocumentIdentifier(uri),
                Character = ">",
                Options = new FormattingOptions(),
            };

            // Act
            var result = await endpoint.Handle(@params, CancellationToken.None);

            // Assert
            Assert.Null(result);
            Assert.False(insertProvider.Called);
        }

        [Fact]
        public async Task Handle_NoApplicableProvider_CallsProviderAndReturnsNull()
        {
            // Arrange
            var codeDocument = CreateCodeDocument();
            var uri = new Uri("file://path/test.razor");
            var documentResolver = CreateDocumentResolver(uri.GetAbsoluteOrUNCPath(), codeDocument);
            var insertProvider = new TestOnAutoInsertProvider(">", canResolve: false);
            var endpoint = new OnAutoInsertEndpoint(Dispatcher, documentResolver, new[] { insertProvider });
            var @params = new OnAutoInsertParams()
            {
                TextDocument = new TextDocumentIdentifier(uri),
                Character = ">",
                Options = new FormattingOptions(),
            };

            // Act
            var result = await endpoint.Handle(@params, CancellationToken.None);

            // Assert
            Assert.Null(result);
            Assert.True(insertProvider.Called);
        }

        private class TestOnAutoInsertProvider : RazorOnAutoInsertProvider
        {
            private readonly bool _canResolve;

            public TestOnAutoInsertProvider(string triggerCharacter, bool canResolve)
            {
                TriggerCharacter = triggerCharacter;
                _canResolve = canResolve;
            }

            public bool Called { get; private set; }

            public TextEdit ResolvedTextEdit { get; set; }

            public override string TriggerCharacter { get; }

            public override bool TryResolveInsertion(Position position, FormattingContext context, out TextEdit edit, out InsertTextFormat format)
            {
                Called = true;
                edit = ResolvedTextEdit;
                format = default;
                return _canResolve;
            }
        }

        private static RazorCodeDocument CreateCodeDocument()
        {
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var emptySourceDocument = RazorSourceDocument.Create(content: string.Empty, fileName: "testFile.razor");
            var syntaxTree = RazorSyntaxTree.Parse(emptySourceDocument);
            codeDocument.SetSyntaxTree(syntaxTree);
            return codeDocument;
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
            documentResolver.Setup(resolver => resolver.TryResolveDocument(documentPath, out documentSnapshot))
                .Returns(true);
            return documentResolver.Object;
        }
    }
}

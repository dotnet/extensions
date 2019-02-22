// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.Test.Infrastructure;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class RazorCompletionEndpointTest : TestBase
    {
        public RazorCompletionEndpointTest()
        {
            // Working around strong naming restriction.
            CompletionFactsService = new DefaultRazorCompletionFactsService();
            TagHelperCompletionService = Mock.Of<TagHelperCompletionService>(
                service => service.GetCompletionsAt(It.IsAny<SourceSpan>(), It.IsAny<RazorCodeDocument>()) == Array.Empty<CompletionItem>());
            TagHelperDescriptionFactory = Mock.Of<TagHelperDescriptionFactory>();
            EmptyDocumentResolver = Mock.Of<DocumentResolver>();
        }

        private RazorCompletionFactsService CompletionFactsService { get; }

        private TagHelperCompletionService TagHelperCompletionService { get; }

        private TagHelperDescriptionFactory TagHelperDescriptionFactory { get; }

        private DocumentResolver EmptyDocumentResolver { get; }

        [Fact]
        public async Task Handle_TagHelperCompletion_ReturnsCompletionItemWithDocumentation()
        {
            // Arrange
            var descriptionFactory = new Mock<TagHelperDescriptionFactory>();
            var markdown = "Some Markdown";
            descriptionFactory.Setup(factory => factory.TryCreateDescription(It.IsAny<ElementDescriptionInfo>(), out markdown))
                .Returns(true);
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, EmptyDocumentResolver, CompletionFactsService, TagHelperCompletionService, descriptionFactory.Object, LoggerFactory);
            var completionItem = new CompletionItem();
            completionItem.SetDescriptionData(ElementDescriptionInfo.Default);

            // Act
            var newCompletionItem = await completionEndpoint.Handle(completionItem, default);

            // Assert
            Assert.NotNull(newCompletionItem.Documentation);
        }

        [Fact]
        public async Task Handle_NonTagHelperCompletion_Noops()
        {
            // Arrange
            var descriptionFactory = new Mock<TagHelperDescriptionFactory>();
            var markdown = "Some Markdown";
            descriptionFactory.Setup(factory => factory.TryCreateDescription(It.IsAny<ElementDescriptionInfo>(), out markdown))
                .Returns(true);
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, EmptyDocumentResolver, CompletionFactsService, TagHelperCompletionService, descriptionFactory.Object, LoggerFactory);
            var completionItem = new CompletionItem();

            // Act
            var newCompletionItem = await completionEndpoint.Handle(completionItem, default);

            // Assert
            Assert.Null(newCompletionItem.Documentation);
        }

        [Fact]
        public void CanResolve_TagHelperElementCompletion_ReturnsTrue()
        {
            // Arrange
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, EmptyDocumentResolver, CompletionFactsService, TagHelperCompletionService, TagHelperDescriptionFactory, LoggerFactory);
            var completionItem = new CompletionItem();
            completionItem.SetDescriptionData(ElementDescriptionInfo.Default);

            // Act
            var result = completionEndpoint.CanResolve(completionItem);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanResolve_NonTagHelperCompletion_ReturnsFalse()
        {
            // Arrange
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, EmptyDocumentResolver, CompletionFactsService, TagHelperCompletionService, TagHelperDescriptionFactory, LoggerFactory);
            var completionItem = new CompletionItem();

            // Act
            var result = completionEndpoint.CanResolve(completionItem);

            // Assert
            Assert.False(result);
        }

        // This is more of an integration test to validate that all the pieces work together
        [Fact]
        public async Task Handle_Unsupported_NoCompletionItems()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocument("@");
            codeDocument.SetUnsupported();
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, documentResolver, CompletionFactsService, TagHelperCompletionService, TagHelperDescriptionFactory, LoggerFactory);
            var request = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Position = new Position(0, 1)
            };

            // Act
            var completionList = await Task.Run(() => completionEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(completionList);
        }

        // This is more of an integration test to validate that all the pieces work together
        [Fact]
        public async Task Handle_ResolvesDirectiveCompletionItems()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocument("@");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, documentResolver, CompletionFactsService, TagHelperCompletionService, TagHelperDescriptionFactory, LoggerFactory);
            var request = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Position = new Position(0, 1)
            };

            // Act
            var completionList = await Task.Run(() => completionEndpoint.Handle(request, default));

            // Assert

            // These are the default directives that don't need to be separately registered, they should always be part of the completion list.
            Assert.Contains(completionList, item => item.InsertText == "addTagHelper");
            Assert.Contains(completionList, item => item.InsertText == "removeTagHelper");
            Assert.Contains(completionList, item => item.InsertText == "tagHelperPrefix");
        }

        // This is more of an integration test to validate that all the pieces work together
        [Fact]
        public async Task Handle_ResolvesTagHelperElementCompletionItems()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var tagHelperCompletionItem = new CompletionItem()
            {
                InsertText = "Test"
            };
            var tagHelperCompletionService = Mock.Of<TagHelperCompletionService>(
                service => service.GetCompletionsAt(It.IsAny<SourceSpan>(), It.IsAny<RazorCodeDocument>()) == new[] { tagHelperCompletionItem });
            var codeDocument = CreateCodeDocument("<");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, documentResolver, CompletionFactsService, tagHelperCompletionService, TagHelperDescriptionFactory, LoggerFactory);
            var request = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Position = new Position(0, 1)
            };

            // Act
            var completionList = await Task.Run(() => completionEndpoint.Handle(request, default));

            // Assert
            Assert.Contains(completionList, item => item.InsertText == "Test");
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

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Components;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor.Razor;
using Moq;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;
using Microsoft.CodeAnalysis.Razor.Tooltip;
using Microsoft.AspNetCore.Razor.LanguageServer.Tooltip;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    public class RazorCompletionEndpointTest : LanguageServerTestBase
    {
        public RazorCompletionEndpointTest()
        {
            // Working around strong naming restriction.
            var tagHelperFactsService = new DefaultTagHelperFactsService();
            var tagHelperCompletionService = new DefaultTagHelperCompletionService(tagHelperFactsService);
            var completionProviders = new RazorCompletionItemProvider[]
            {
                new DirectiveCompletionItemProvider(),
                new DirectiveAttributeCompletionItemProvider(tagHelperFactsService),
                new DirectiveAttributeParameterCompletionItemProvider(tagHelperFactsService),
                new TagHelperCompletionProvider(tagHelperCompletionService, new DefaultHtmlFactsService(), tagHelperFactsService)
            };
            CompletionFactsService = new DefaultRazorCompletionFactsService(completionProviders);
            TagHelperTooltipFactory = Mock.Of<TagHelperTooltipFactory>();
            EmptyDocumentResolver = Mock.Of<DocumentResolver>();
        }

        private RazorCompletionFactsService CompletionFactsService { get; }

        private TagHelperTooltipFactory TagHelperTooltipFactory { get; }

        private DocumentResolver EmptyDocumentResolver { get; }

        [Fact]
        public void TryConvert_Directive_ReturnsTrue()
        {
            // Arrange
            var completionItem = new RazorCompletionItem("testDisplay", "testInsert", RazorCompletionItemKind.Directive);
            var description = "Something";
            completionItem.SetDirectiveCompletionDescription(new DirectiveCompletionDescription(description));

            // Act
            var result = RazorCompletionEndpoint.TryConvert(completionItem, out var converted);

            // Assert
            Assert.True(result);
            Assert.Equal(completionItem.DisplayText, converted.Label);
            Assert.Equal(completionItem.InsertText, converted.InsertText);
            Assert.Equal(completionItem.DisplayText, converted.FilterText);
            Assert.Equal(completionItem.DisplayText, converted.SortText);
            Assert.Null(converted.Detail);
            Assert.Equal(description, converted.Documentation.String);
            Assert.True(converted.TryGetRazorCompletionKind(out var convertedKind));
            Assert.Equal(RazorCompletionItemKind.Directive, convertedKind);
        }

        [Fact]
        public void TryConvert_Directive_SerializationDoesNotThrow()
        {
            // Arrange
            var completionItem = new RazorCompletionItem("testDisplay", "testInsert", RazorCompletionItemKind.Directive);
            var description = "Something";
            completionItem.SetDirectiveCompletionDescription(new DirectiveCompletionDescription(description));
            RazorCompletionEndpoint.TryConvert(completionItem, out var converted);

            // Act & Assert
            JsonConvert.SerializeObject(converted);
        }

        [Fact]
        public void TryConvert_DirectiveAttributeTransition_SerializationDoesNotThrow()
        {
            // Arrange
            var completionItem = DirectiveAttributeTransitionCompletionItemProvider.TransitionCompletionItem;
            RazorCompletionEndpoint.TryConvert(completionItem, out var converted);

            // Act & Assert
            JsonConvert.SerializeObject(converted);
        }

        [Fact]
        public void TryConvert_DirectiveAttributeTransition_ReturnsTrue()
        {
            // Arrange
            var completionItem = DirectiveAttributeTransitionCompletionItemProvider.TransitionCompletionItem;
            var description = completionItem.GetDirectiveCompletionDescription().Description;

            // Act
            var result = RazorCompletionEndpoint.TryConvert(completionItem, out var converted);

            // Assert
            Assert.True(result);
            Assert.False(converted.Preselect);
            Assert.Equal(completionItem.DisplayText, converted.Label);
            Assert.Equal(completionItem.InsertText, converted.InsertText);
            Assert.Equal(completionItem.DisplayText, converted.FilterText);
            Assert.Equal(completionItem.DisplayText, converted.SortText);
            Assert.Null(converted.Detail);
            Assert.Equal(description, converted.Documentation.String);
            Assert.NotNull(converted.Command);
            Assert.True(converted.TryGetRazorCompletionKind(out var convertedKind));
            Assert.Equal(RazorCompletionItemKind.Directive, convertedKind);
        }

        [Fact]
        public void TryConvert_MarkupTransition_ReturnsTrue()
        {
            // Arrange
            var completionItem = MarkupTransitionCompletionItemProvider.MarkupTransitionCompletionItem;
            var description = completionItem.GetMarkupTransitionCompletionDescription().Description;

            // Act
            var result = RazorCompletionEndpoint.TryConvert(completionItem, out var converted);

            // Assert
            Assert.True(result);
            Assert.Equal(completionItem.DisplayText, converted.Label);
            Assert.Equal(completionItem.InsertText, converted.InsertText);
            Assert.Equal(completionItem.DisplayText, converted.FilterText);
            Assert.Equal(completionItem.DisplayText, converted.SortText);
            Assert.Null(converted.Detail);
            Assert.Equal(description, converted.Documentation.String);
            Assert.Equal(converted.CommitCharacters, completionItem.CommitCharacters);
        }

        [Fact]
        public void TryConvert_MarkupTransition_SerializationDoesNotThrow()
        {
            // Arrange
            var completionItem = MarkupTransitionCompletionItemProvider.MarkupTransitionCompletionItem;
            RazorCompletionEndpoint.TryConvert(completionItem, out var converted);

            // Act & Assert
            JsonConvert.SerializeObject(converted);
        }

        [Fact]
        public void TryConvert_DirectiveAttribute_ReturnsTrue()
        {
            // Arrange
            var completionItem = new RazorCompletionItem("@testDisplay", "testInsert", RazorCompletionItemKind.DirectiveAttribute, new [] { "=", ":" });
            completionItem.SetAttributeCompletionDescription(new AggregateBoundAttributeDescription(Array.Empty<BoundAttributeDescriptionInfo>()));

            // Act
            var result = RazorCompletionEndpoint.TryConvert(completionItem, out var converted);

            // Assert
            Assert.True(result);
            Assert.Equal(completionItem.DisplayText, converted.Label);
            Assert.Equal(completionItem.InsertText, converted.InsertText);
            Assert.Equal(completionItem.InsertText, converted.FilterText);
            Assert.Equal(completionItem.InsertText, converted.SortText);
            Assert.Equal(completionItem.CommitCharacters, converted.CommitCharacters);
            Assert.Null(converted.Detail);
            Assert.Null(converted.Documentation);
            Assert.Null(converted.Command);
            Assert.True(converted.TryGetRazorCompletionKind(out var convertedKind));
            Assert.Equal(RazorCompletionItemKind.DirectiveAttribute, convertedKind);
        }

        [Fact]
        public void TryConvert_DirectiveAttributeParameter_ReturnsTrue()
        {
            // Arrange
            var completionItem = new RazorCompletionItem("format", "format", RazorCompletionItemKind.DirectiveAttributeParameter);
            completionItem.SetAttributeCompletionDescription(new AggregateBoundAttributeDescription(Array.Empty<BoundAttributeDescriptionInfo>()));

            // Act
            var result = RazorCompletionEndpoint.TryConvert(completionItem, out var converted);

            // Assert
            Assert.True(result);
            Assert.Equal(completionItem.DisplayText, converted.Label);
            Assert.Equal(completionItem.InsertText, converted.InsertText);
            Assert.Equal(completionItem.InsertText, converted.FilterText);
            Assert.Equal(completionItem.InsertText, converted.SortText);
            Assert.Null(converted.Detail);
            Assert.Null(converted.Documentation);
            Assert.Null(converted.Command);
            Assert.True(converted.TryGetRazorCompletionKind(out var convertedKind));
            Assert.Equal(RazorCompletionItemKind.DirectiveAttributeParameter, convertedKind);
        }

        [Fact]
        public void TryConvert_TagHelperElement_ReturnsTrue()
        {
            // Arrange
            var completionItem = new RazorCompletionItem("format", "format", RazorCompletionItemKind.TagHelperElement);
            completionItem.SetTagHelperElementDescriptionInfo(new AggregateBoundElementDescription(Array.Empty<BoundElementDescriptionInfo>()));

            // Act
            var result = RazorCompletionEndpoint.TryConvert(completionItem, out var converted);

            // Assert
            Assert.True(result);
            Assert.Equal(completionItem.DisplayText, converted.Label);
            Assert.Equal(completionItem.InsertText, converted.InsertText);
            Assert.Equal(completionItem.InsertText, converted.FilterText);
            Assert.Equal(completionItem.InsertText, converted.SortText);
            Assert.Null(converted.Detail);
            Assert.Null(converted.Documentation);
            Assert.Null(converted.Command);
            var descriptionInfo = completionItem.GetTagHelperElementDescriptionInfo();
            Assert.NotNull(descriptionInfo);
        }

        [Fact]
        public void TryConvert_TagHelperAttribute_ReturnsTrue()
        {
            // Arrange
            var completionItem = new RazorCompletionItem("format", "format", RazorCompletionItemKind.TagHelperAttribute);
            completionItem.SetAttributeCompletionDescription(new AggregateBoundAttributeDescription(Array.Empty<BoundAttributeDescriptionInfo>()));

            // Act
            var result = RazorCompletionEndpoint.TryConvert(completionItem, out var converted);

            // Assert
            Assert.True(result);
            Assert.Equal(completionItem.DisplayText, converted.Label);
            Assert.Equal(completionItem.InsertText, converted.InsertText);
            Assert.Equal(completionItem.InsertText, converted.FilterText);
            Assert.Equal(completionItem.InsertText, converted.SortText);
            Assert.Null(converted.Detail);
            Assert.Null(converted.Documentation);
            Assert.Null(converted.Command);
            var descriptionInfo = completionItem.GetAttributeCompletionDescription();
            Assert.NotNull(descriptionInfo);
        }

        [Fact]
        public async Task Handle_DirectiveAttributeCompletion_ReturnsCompletionItemWithDocumentation()
        {
            // Arrange
            var descriptionFactory = new Mock<TagHelperTooltipFactory>();
            var markdown = new MarkupContent
            {
                Kind = MarkupKind.Markdown,
                Value = "Some Markdown"
            };
            descriptionFactory.Setup(factory => factory.TryCreateTooltip(It.IsAny<AggregateBoundAttributeDescription>(), out markdown))
                .Returns(true);
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, EmptyDocumentResolver, CompletionFactsService, descriptionFactory.Object, LoggerFactory);
            var completionItem = new CompletionItem();
            completionItem.SetDescriptionInfo(new AggregateBoundAttributeDescription(Array.Empty<BoundAttributeDescriptionInfo>()));
            completionItem.SetRazorCompletionKind(RazorCompletionItemKind.DirectiveAttribute);

            // Act
            var newCompletionItem = await completionEndpoint.Handle(completionItem, default);

            // Assert
            Assert.NotNull(newCompletionItem.Documentation);
        }

        [Fact]
        public async Task Handle_DirectiveAttributeParameterCompletion_ReturnsCompletionItemWithDocumentation()
        {
            // Arrange
            var descriptionFactory = new Mock<TagHelperTooltipFactory>();
            var markdown = new MarkupContent
            {
                Kind = MarkupKind.Markdown,
                Value = "Some Markdown"
            };
            descriptionFactory.Setup(factory => factory.TryCreateTooltip(It.IsAny<AggregateBoundAttributeDescription>(), out markdown))
                .Returns(true);
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, EmptyDocumentResolver, CompletionFactsService, descriptionFactory.Object, LoggerFactory);
            var completionItem = new CompletionItem();
            completionItem.SetDescriptionInfo(new AggregateBoundAttributeDescription(Array.Empty<BoundAttributeDescriptionInfo>()));
            completionItem.SetRazorCompletionKind(RazorCompletionItemKind.DirectiveAttributeParameter);

            // Act
            var newCompletionItem = await completionEndpoint.Handle(completionItem, default);

            // Assert
            Assert.NotNull(newCompletionItem.Documentation);
        }

        [Fact]
        public async Task Handle_TagHelperElementCompletion_ReturnsCompletionItemWithDocumentation()
        {
            // Arrange
            var descriptionFactory = new Mock<TagHelperTooltipFactory>();
            var markdown = new MarkupContent
            {
                Kind = MarkupKind.Markdown,
                Value = "Some Markdown"
            };
            descriptionFactory.Setup(factory => factory.TryCreateTooltip(It.IsAny<AggregateBoundElementDescription>(), out markdown))
                .Returns(true);
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, EmptyDocumentResolver, CompletionFactsService, descriptionFactory.Object, LoggerFactory);
            var completionItem = new CompletionItem();
            completionItem.SetDescriptionInfo(AggregateBoundElementDescription.Default);

            // Act
            var newCompletionItem = await completionEndpoint.Handle(completionItem, default);

            // Assert
            Assert.NotNull(newCompletionItem.Documentation);
        }

        [Fact]
        public async Task Handle_TagHelperAttribute_ReturnsCompletionItemWithDocumentation()
        {
            // Arrange
            var descriptionFactory = new Mock<TagHelperTooltipFactory>();
            var markdown = new MarkupContent{
                Kind = MarkupKind.Markdown,
                Value = "Some Markdown"
            };
            descriptionFactory.Setup(factory => factory.TryCreateTooltip(It.IsAny<AggregateBoundAttributeDescription>(), out markdown))
                .Returns(true);
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, EmptyDocumentResolver, CompletionFactsService, descriptionFactory.Object, LoggerFactory);
            var completionItem = new CompletionItem();
            completionItem.SetDescriptionInfo(AggregateBoundAttributeDescription.Default);
            completionItem.SetRazorCompletionKind(RazorCompletionItemKind.TagHelperAttribute);

            // Act
            var newCompletionItem = await completionEndpoint.Handle(completionItem, default);

            // Assert
            Assert.NotNull(newCompletionItem.Documentation);
        }

        [Fact]
        public async Task Handle_NonTagHelperCompletion_Noops()
        {
            // Arrange
            var descriptionFactory = new Mock<TagHelperTooltipFactory>();
            var markdown = new MarkupContent
            {
                Kind = MarkupKind.Markdown,
                Value = "Some Markdown"
            };
            descriptionFactory.Setup(factory => factory.TryCreateTooltip(It.IsAny<AggregateBoundElementDescription>(), out markdown))
                .Returns(true);
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, EmptyDocumentResolver, CompletionFactsService, descriptionFactory.Object, LoggerFactory);
            var completionItem = new CompletionItem();

            // Act
            var newCompletionItem = await completionEndpoint.Handle(completionItem, default);

            // Assert
            Assert.Null(newCompletionItem.Documentation);
        }

        [Fact]
        public void CanResolve_DirectiveAttributeCompletion_ReturnsTrue()
        {
            // Arrange
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, EmptyDocumentResolver, CompletionFactsService, TagHelperTooltipFactory, LoggerFactory);
            var completionItem = new CompletionItem();
            completionItem.SetRazorCompletionKind(RazorCompletionItemKind.DirectiveAttribute);

            // Act
            var result = completionEndpoint.CanResolve(completionItem);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanResolve_DirectiveAttributeParameterCompletion_ReturnsTrue()
        {
            // Arrange
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, EmptyDocumentResolver, CompletionFactsService, TagHelperTooltipFactory, LoggerFactory);
            var completionItem = new CompletionItem();
            completionItem.SetRazorCompletionKind(RazorCompletionItemKind.DirectiveAttributeParameter);

            // Act
            var result = completionEndpoint.CanResolve(completionItem);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanResolve_TagHelperElementCompletion_ReturnsTrue()
        {
            // Arrange
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, EmptyDocumentResolver, CompletionFactsService, TagHelperTooltipFactory, LoggerFactory);
            var completionItem = new CompletionItem();
            completionItem.SetDescriptionInfo(AggregateBoundElementDescription.Default);

            // Act
            var result = completionEndpoint.CanResolve(completionItem);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanResolve_TagHelperAttribute_ReturnsTrue()
        {
            // Arrange
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, EmptyDocumentResolver, CompletionFactsService, TagHelperTooltipFactory, LoggerFactory);
            var completionItem = new CompletionItem();
            completionItem.SetDescriptionInfo(AggregateBoundAttributeDescription.Default);
            completionItem.SetRazorCompletionKind(RazorCompletionItemKind.TagHelperAttribute);

            // Act
            var result = completionEndpoint.CanResolve(completionItem);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanResolve_NonTagHelperCompletion_ReturnsFalse()
        {
            // Arrange
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, EmptyDocumentResolver, CompletionFactsService, TagHelperTooltipFactory, LoggerFactory);
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
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, documentResolver, CompletionFactsService, TagHelperTooltipFactory, LoggerFactory);
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
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, documentResolver, CompletionFactsService, TagHelperTooltipFactory, LoggerFactory);
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
            var builder = TagHelperDescriptorBuilder.Create(ComponentMetadata.Component.TagHelperKind, "TestTagHelper", "TestAssembly");
            builder.TagMatchingRule(rule => rule.TagName = "Test");
            builder.SetTypeName("TestNamespace.TestTagHelper");
            var tagHelper = builder.Build();
            var tagHelperContext = TagHelperDocumentContext.Create(prefix: string.Empty, new[] { tagHelper });
            var codeDocument = CreateCodeDocument("<");
            codeDocument.SetTagHelperContext(tagHelperContext);
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, documentResolver, CompletionFactsService, TagHelperTooltipFactory, LoggerFactory);
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

        // This is more of an integration test to validate that all the pieces work together
        [Fact]
        public async Task Handle_ResolvesTagHelperAttributeItems()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var builder = TagHelperDescriptorBuilder.Create(ComponentMetadata.Component.TagHelperKind, "TestTagHelper", "TestAssembly");
            builder.TagMatchingRule(rule => rule.TagName = "*");
            builder.BindAttribute(attribute =>
            {
                attribute.Name = "testAttribute";
                attribute.TypeName = typeof(string).FullName;
                attribute.SetPropertyName("TestAttribute");
            });
            builder.SetTypeName("TestNamespace.TestTagHelper");
            var tagHelper = builder.Build();
            var tagHelperContext = TagHelperDocumentContext.Create(prefix: string.Empty, new[] { tagHelper });
            var codeDocument = CreateCodeDocument("<test  ");
            codeDocument.SetTagHelperContext(tagHelperContext);
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var completionEndpoint = new RazorCompletionEndpoint(Dispatcher, documentResolver, CompletionFactsService, TagHelperTooltipFactory, LoggerFactory);
            var request = new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Position = new Position(0, 6)
            };

            // Act
            var completionList = await Task.Run(() => completionEndpoint.Handle(request, default));

            // Assert
            Assert.Contains(completionList, item => item.InsertText == "testAttribute");
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
            var tagHelperDocumentContext = TagHelperDocumentContext.Create(prefix: string.Empty, Enumerable.Empty<TagHelperDescriptor>());
            codeDocument.SetTagHelperContext(tagHelperDocumentContext);
            return codeDocument;
        }
    }
}

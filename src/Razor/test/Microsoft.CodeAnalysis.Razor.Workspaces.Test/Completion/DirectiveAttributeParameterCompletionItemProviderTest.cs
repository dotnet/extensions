// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.VisualStudio.Editor.Razor;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Completion
{
    public class DirectiveAttributeParameterCompletionItemProviderTest : RazorIntegrationTestBase
    {
        internal override string FileKind => FileKinds.Component;

        internal override bool UseTwoPhaseCompilation => true;

        public DirectiveAttributeParameterCompletionItemProviderTest()
        {
            Provider = new DirectiveAttributeParameterCompletionItemProvider(new DefaultTagHelperFactsService());
            var codeDocument = GetCodeDocument(string.Empty);
            DefaultTagHelperDocumentContext = codeDocument.GetTagHelperContext();
            EmptyAttributes = Enumerable.Empty<string>();
        }

        private DirectiveAttributeParameterCompletionItemProvider Provider { get; }

        private TagHelperDocumentContext DefaultTagHelperDocumentContext { get; }

        private IEnumerable<string> EmptyAttributes { get; }

        private RazorCodeDocument GetCodeDocument(string content)
        {
            var result = CompileToCSharp(content, throwOnFailure: false);
            return result.CodeDocument;
        }

        [Fact]
        public void GetCompletionItems_LocationHasNoOwner_ReturnsEmptyCollection()
        {
            // Arrange
            var codeDocument = GetCodeDocument("<input @  />");
            var syntaxTree = codeDocument.GetSyntaxTree();
            var tagHelperDocumentContext = codeDocument.GetTagHelperContext();
            var span = new SourceSpan(30, 0);

            // Act
            var completions = Provider.GetCompletionItems(syntaxTree, tagHelperDocumentContext, span);

            // Assert
            Assert.Empty(completions);
        }

        [Fact]
        public void GetCompletionItems_OnNonAttributeArea_ReturnsEmptyCollection()
        {
            // Arrange
            var codeDocument = GetCodeDocument("<input @  />");
            var syntaxTree = codeDocument.GetSyntaxTree();
            var tagHelperDocumentContext = codeDocument.GetTagHelperContext();
            var span = new SourceSpan(3, 0);

            // Act
            var completions = Provider.GetCompletionItems(syntaxTree, tagHelperDocumentContext, span);

            // Assert
            Assert.Empty(completions);
        }

        [Fact]
        public void GetCompletionItems_OnDirectiveAttributeName_ReturnsEmptyCollection()
        {
            // Arrange
            var codeDocument = GetCodeDocument("<input @bind:fo  />");
            var syntaxTree = codeDocument.GetSyntaxTree();
            var tagHelperDocumentContext = codeDocument.GetTagHelperContext();
            var span = new SourceSpan(8, 0);

            // Act
            var completions = Provider.GetCompletionItems(syntaxTree, tagHelperDocumentContext, span);

            // Assert
            Assert.Empty(completions);
        }

        [Fact]
        public void GetCompletionItems_OnDirectiveAttributeParameter_ReturnsCompletions()
        {
            // Arrange
            var codeDocument = GetCodeDocument("<input @bind:fo  />");
            var syntaxTree = codeDocument.GetSyntaxTree();
            var tagHelperDocumentContext = codeDocument.GetTagHelperContext();
            var expectedCompletions = new[] {
                new RazorCompletionItem("event", "event", string.Empty, RazorCompletionItemKind.DirectiveAttributeParameter),
                new RazorCompletionItem("format", "format", string.Empty, RazorCompletionItemKind.DirectiveAttributeParameter),
            };
            var span = new SourceSpan(14, 0);

            // Act
            var completions = Provider.GetCompletionItems(syntaxTree, tagHelperDocumentContext, span);

            // Assert
            var orderedCompletions = completions.OrderBy(c => c.DisplayText);
            Assert.Equal(expectedCompletions, orderedCompletions);
        }

        [Fact]
        public void GetAttributeParameterCompletions_NoDescriptorsForTag_ReturnsEmptyCollection()
        {
            // Arrange
            var documentContext = TagHelperDocumentContext.Create(string.Empty, Enumerable.Empty<TagHelperDescriptor>());

            // Act
            var completions = Provider.GetAttributeParameterCompletions("@bin", string.Empty, "foobarbaz", EmptyAttributes, documentContext);

            // Assert
            Assert.Empty(completions);
        }

        [Fact]
        public void GetAttributeParameterCompletions_NoDirectiveAttributesForTag_ReturnsEmptyCollection()
        {
            // Arrange
            var descriptor = TagHelperDescriptorBuilder.Create("CatchAll", "TestAssembly");
            descriptor.BoundAttributeDescriptor(boundAttribute => boundAttribute.Name = "Test");
            descriptor.TagMatchingRule(rule => rule.RequireTagName("*"));
            var documentContext = TagHelperDocumentContext.Create(string.Empty, new[] { descriptor.Build() });

            // Act
            var completions = Provider.GetAttributeParameterCompletions("@bin", string.Empty, "input", EmptyAttributes, documentContext);

            // Assert
            Assert.Empty(completions);
        }

        [Fact]
        public void GetAttributeParameterCompletions_SelectedDirectiveAttributeParameter_IsExcludedInCompletions()
        {
            // Arrange
            var expectedCompletion = new RazorCompletionItem("format", "format", string.Empty, RazorCompletionItemKind.DirectiveAttributeParameter);
            var attributeNames = new string[] { "@bind" };

            // Act
            var completions = Provider.GetAttributeParameterCompletions("@bind", "format", "input", attributeNames, DefaultTagHelperDocumentContext);

            // Assert
            Assert.DoesNotContain(expectedCompletion, completions);
        }

        [Fact]
        public void GetAttributeParameterCompletions_ReturnsCompletion()
        {
            // Arrange
            var expectedCompletion = new RazorCompletionItem("format", "format", string.Empty, RazorCompletionItemKind.DirectiveAttributeParameter);

            // Act
            var completions = Provider.GetAttributeParameterCompletions("@bind", string.Empty, "input", EmptyAttributes, DefaultTagHelperDocumentContext);

            // Assert
            Assert.Contains(expectedCompletion, completions);
        }

        [Fact]
        public void GetAttributeParameterCompletions_BaseDirectiveAttributeAndParameterVariationsExist_ExcludesCompletion()
        {
            // Arrange
            var expectedCompletion = new RazorCompletionItem("format", "format", string.Empty, RazorCompletionItemKind.DirectiveAttributeParameter);
            var attributeNames = new[]
            {
                "@bind",
                "@bind:format",
                "@bind:event",
                "@",
            };

            // Act
            var completions = Provider.GetAttributeParameterCompletions("@bind", string.Empty, "input", attributeNames, DefaultTagHelperDocumentContext);

            // Assert
            Assert.DoesNotContain(expectedCompletion, completions);
        }
    }
}

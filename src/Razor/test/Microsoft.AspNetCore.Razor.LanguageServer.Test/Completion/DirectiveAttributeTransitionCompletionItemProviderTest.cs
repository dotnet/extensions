// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Completion;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    public class DirectiveAttributeTransitionCompletionItemProviderTest
    {
        public DirectiveAttributeTransitionCompletionItemProviderTest()
        {
            TagHelperDocumentContext = TagHelperDocumentContext.Create(prefix: string.Empty, Array.Empty<TagHelperDescriptor>());
            Provider = new DirectiveAttributeTransitionCompletionItemProvider();
        }

        private TagHelperDocumentContext TagHelperDocumentContext { get; }

        private DirectiveAttributeTransitionCompletionItemProvider Provider { get; }

        private RazorCompletionItem TransitionCompletionItem => DirectiveAttributeTransitionCompletionItemProvider.TransitionCompletionItem;

        [Fact]
        public void GetCompletionItems_AttributeAreaInNonComponentFile_ReturnsEmptyList()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree("<input  />", FileKinds.Legacy);
            var location = new SourceSpan(7, 0);

            // Act
            var result = Provider.GetCompletionItems(syntaxTree, TagHelperDocumentContext, location);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetCompletionItems_OutsideOfFile_ReturnsEmptyList()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree("<input  />");
            var location = new SourceSpan(50, 0);

            // Act
            var result = Provider.GetCompletionItems(syntaxTree, TagHelperDocumentContext, location);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetCompletionItems_NonAttribute_ReturnsEmptyList()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree("<input  />");
            var location = new SourceSpan(2, 0);

            // Act
            var result = Provider.GetCompletionItems(syntaxTree, TagHelperDocumentContext, location);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetCompletionItems_ExistingAttribute_ReturnsEmptyList()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree("<input @ />");
            var location = new SourceSpan(8, 0);

            // Act
            var result = Provider.GetCompletionItems(syntaxTree, TagHelperDocumentContext, location);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetCompletionItems_AttributeAreaInComponentFile_ReturnsTransitionCompletionItem()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree("<input  />");
            var location = new SourceSpan(7, 0);

            // Act
            var result = Provider.GetCompletionItems(syntaxTree, TagHelperDocumentContext, location);

            // Assert
            var item = Assert.Single(result);
            Assert.Same(item, TransitionCompletionItem);
        }

        private static RazorSyntaxTree GetSyntaxTree(string text, string fileKind = null)
        {
            fileKind = fileKind ?? FileKinds.Component;
            var sourceDocument = TestRazorSourceDocument.Create(text);
            var projectEngine = RazorProjectEngine.Create(builder => { });
            var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, fileKind, Array.Empty<RazorSourceDocument>(), Array.Empty<TagHelperDescriptor>());
            var syntaxTree = codeDocument.GetSyntaxTree();

            return syntaxTree;
        }
    }
}

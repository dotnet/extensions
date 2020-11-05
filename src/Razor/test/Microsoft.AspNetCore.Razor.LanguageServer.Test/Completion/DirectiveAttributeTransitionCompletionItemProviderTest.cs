// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
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
        public void IsValidCompletionPoint_AtPrefixLeadingEdge_ReturnsFalse()
        {
            // Arrange

            // <p| class=""></p>
            var location = new SourceSpan(2, 0);
            var prefixLocation = new TextSpan(2, 1);
            var attributeNameLocation = new TextSpan(3, 5);

            // Act
            var result = DirectiveAttributeTransitionCompletionItemProvider.IsValidCompletionPoint(location, prefixLocation, attributeNameLocation);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidCompletionPoint_WithinPrefix_ReturnsTrue()
        {
            // Arrange

            // <p | class=""></p>
            var location = new SourceSpan(3, 0);
            var prefixLocation = new TextSpan(2, 2);
            var attributeNameLocation = new TextSpan(4, 5);

            // Act
            var result = DirectiveAttributeTransitionCompletionItemProvider.IsValidCompletionPoint(location, prefixLocation, attributeNameLocation);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidCompletionPoint_NullPrefix_ReturnsFalse()
        {
            // Arrange

            // <svg xml:base="abc"xm| ></svg>
            var location = new SourceSpan(21, 0);
            TextSpan? prefixLocation = null;
            var attributeNameLocation = new TextSpan(4, 5);

            // Act
            var result = DirectiveAttributeTransitionCompletionItemProvider.IsValidCompletionPoint(location, prefixLocation, attributeNameLocation);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidCompletionPoint_AtNameLeadingEdge_ReturnsFalse()
        {
            // Arrange

            // <p |class=""></p>
            var location = new SourceSpan(3, 0);
            var prefixLocation = new TextSpan(2, 1);
            var attributeNameLocation = new TextSpan(3, 5);

            // Act
            var result = DirectiveAttributeTransitionCompletionItemProvider.IsValidCompletionPoint(location, prefixLocation, attributeNameLocation);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidCompletionPoint_WithinName_ReturnsFalse()
        {
            // Arrange

            // <p cl|ass=""></p>
            var location = new SourceSpan(5, 0);
            var prefixLocation = new TextSpan(2, 1);
            var attributeNameLocation = new TextSpan(3, 5);

            // Act
            var result = DirectiveAttributeTransitionCompletionItemProvider.IsValidCompletionPoint(location, prefixLocation, attributeNameLocation);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidCompletionPoint_OutsideOfNameAndPrefix_ReturnsFalse()
        {
            // Arrange

            // <p class=|""></p>
            var location = new SourceSpan(9, 0);
            var prefixLocation = new TextSpan(2, 1);
            var attributeNameLocation = new TextSpan(3, 5);

            // Act
            var result = DirectiveAttributeTransitionCompletionItemProvider.IsValidCompletionPoint(location, prefixLocation, attributeNameLocation);

            // Assert
            Assert.False(result);
        }

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
        public void GetCompletionItems_InbetweenSelfClosingEnd_ReturnsEmptyList()
        {
            // Arrange

            var syntaxTree = GetSyntaxTree("<input /" + Environment.NewLine);
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

        [Fact]
        public void GetCompletionItems_AttributeAreaEndOfSelfClosingTag_ReturnsTransitionCompletionItem()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree("<input />");
            var location = new SourceSpan(7, 0);

            // Act
            var result = Provider.GetCompletionItems(syntaxTree, TagHelperDocumentContext, location);

            // Assert
            var item = Assert.Single(result);
            Assert.Same(item, TransitionCompletionItem);
        }

        [Fact]
        public void GetCompletionItems_AttributeAreaEndOfOpeningTag_ReturnsTransitionCompletionItem()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree("<input ></input>");
            var location = new SourceSpan(7, 0);

            // Act
            var result = Provider.GetCompletionItems(syntaxTree, TagHelperDocumentContext, location);

            // Assert
            var item = Assert.Single(result);
            Assert.Same(item, TransitionCompletionItem);
        }

        [Fact]
        public void GetCompletionItems_ExistingAttribute_LeadingEdge_ReturnsEmptyList()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree("<input src=\"xyz\" />");
            var location = new SourceSpan(7, 0);

            // Act
            var result = Provider.GetCompletionItems(syntaxTree, TagHelperDocumentContext, location);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetCompletionItems_ExistingAttribute_TrailingEdge_ReturnsEmptyList()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree("<input src=\"xyz\" />");
            var location = new SourceSpan(16, 0);

            // Act
            var result = Provider.GetCompletionItems(syntaxTree, TagHelperDocumentContext, location);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetCompletionItems_ExistingAttribute_Partial_ReturnsEmptyList()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree("<svg xml: ></svg>");
            var location = new SourceSpan(9, 0);

            // Act
            var result = Provider.GetCompletionItems(syntaxTree, TagHelperDocumentContext, location);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetCompletionItems_AttributeAreaInIncompleteAttributeTransition_ReturnsTransitionCompletionItem()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree("<input   @{");
            var location = new SourceSpan(7, 0);

            // Act
            var result = Provider.GetCompletionItems(syntaxTree, TagHelperDocumentContext, location);

            // Assert
            var item = Assert.Single(result);
            Assert.Same(item, TransitionCompletionItem);
        }

        [Fact]
        public void GetCompletionItems_AttributeAreaInIncompleteComponent_ReturnsTransitionCompletionItem()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree("<svg  xml:base=\"d\"></svg>");
            var location = new SourceSpan(5, 0);

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

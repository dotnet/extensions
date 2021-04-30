// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.CodeAnalysis.Razor.Completion
{
    public class MarkupTransitionCompletionItemProviderTest
    {
        public MarkupTransitionCompletionItemProviderTest()
        {
            Provider = new MarkupTransitionCompletionItemProvider(new DefaultHtmlFactsService());
        }

        private MarkupTransitionCompletionItemProvider Provider { get; }

        [Fact]
        public void GetCompletionItems_ReturnsEmptyCompletionItemInUnopenedMarkupContext()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("<div>");
            var location = new SourceSpan(5, 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Empty(completionItems);
        }

        [Fact]
        public void GetCompletionItems_ReturnsEmptyCompletionItemInSimpleMarkupContext()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("<div><");
            var location = new SourceSpan(6, 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Empty(completionItems);
        }

        [Fact]
        public void GetCompletionItems_ReturnsEmptyCompletionItemInNestedMarkupContext()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("<div><span><p></p><p>< </p></span></div>");
            var location = new SourceSpan(22, 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Empty(completionItems);
        }

        [Fact]
        public void GetCompletionItems_ReturnsMarkupTransitionCompletionItemInCodeBlockStartingTag()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("@{<");
            var location = new SourceSpan(3, 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Collection(completionItems, AssertRazorCompletionItem);
        }

        [Fact]
        public void GetCompletionItems_ReturnsMarkupTransitionCompletionItemInCodeBlockPartialCompletion()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("@{<te");
            var location = new SourceSpan(5, 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Collection(completionItems, AssertRazorCompletionItem);
        }

        [Fact]
        public void GetCompletionItems_ReturnsMarkupTransitionCompletionItemInIfConditional()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("@if (true) {< }");
            var location = new SourceSpan(13, 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Collection(completionItems, AssertRazorCompletionItem);
        }

        [Fact]
        public void GetCompletionItems_ReturnsMarkupTransitionCompletionItemInFunctionDirective()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("@functions {public string GetHello(){< return \"pi\";}}", FunctionsDirective.Directive);
            var location = new SourceSpan(38, 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Collection(completionItems, AssertRazorCompletionItem);
        }

        [Fact]
        public void GetCompletionItems_ReturnsEmptyCompletionItemInExpression()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree(@"@{
    SomeFunctionAcceptingMethod(() =>
    {
        string foo = ""bar"";
    });
}

@SomeFunctionAcceptingMethod(() =>
{
    <
})");
            var location = new SourceSpan(121 + (Environment.NewLine.Length * 9), 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Empty(completionItems);
        }

        [Fact]
        public void GetCompletionItems_ReturnsEmptyCompletionItemInSingleLineTransitions()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree(@"@{
    @* @: Here's some Markup | <-- You shouldn't get a <text> tag completion here. *@
    @: Here's some markup <
}");
            var location = new SourceSpan(114 + (Environment.NewLine.Length * 2), 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Empty(completionItems);
        }

        [Fact]
        public void GetCompletionItems_ReturnsMarkupTransitionCompletionItemInNestedCSharpBlock()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree(@"<div>
@if (true)
{
  < @* Should get text completion here *@
}
</div>");
            var location = new SourceSpan(19 + (Environment.NewLine.Length * 3), 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Collection(completionItems, AssertRazorCompletionItem);
        }

        [Fact]
        public void GetCompletionItems_ReturnsEmptyCompletionItemInNestedMarkupBlock()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree(@"@if (true)
{
<div>
  < @* Shouldn't get text completion here *@
</div>
}");
            var location = new SourceSpan(19 + (Environment.NewLine.Length * 3), 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Empty(completionItems);
        }

        [Fact]
        public void GetCompletionItems_ReturnsMarkupTransitionCompletionItemWithUnrelatedClosingAngleBracket()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree(@"@functions {
    public void SomeOtherMethod()
    {
        <
    }

    private bool _collapseNavMenu => true;
}", FunctionsDirective.Directive);
            var location = new SourceSpan(59 + (Environment.NewLine.Length * 3), 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Collection(completionItems, AssertRazorCompletionItem);
        }

        [Fact]
        public void GetCompletionItems_ReturnsMarkupTransitionCompletionItemWithUnrelatedClosingTag()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("@{<></>");
            var location = new SourceSpan(3, 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Collection(completionItems, AssertRazorCompletionItem);
        }

        [Fact]
        public void GetCompletionItems_ReturnsEmptyCompletionItemIfSyntaxTreeNull()
        {
            // Arrange
            var location = new SourceSpan(0, 0);

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => Provider.GetCompletionItems(null, null, location));
        }

        [Fact]
        public void GetCompletionItems_ReturnsEmptyCompletionItemWhenOwnerIsComplexExpression()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("@DateTime.Now<");
            var location = new SourceSpan(14, 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Empty(completionItems);
        }

        [Fact]
        public void GetCompletionItems_ReturnsEmptyCompletionItemWhenOwnerIsExplicitExpression()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("@(something)<");
            var location = new SourceSpan(13, 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Empty(completionItems);
        }

        [Fact]
        public void GetCompletionItems_ReturnsEmptyCompletionItemWithSpaceAfterStartTag()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("@{< ");
            var location = new SourceSpan(4, 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Empty(completionItems);
        }

        [Fact]
        public void GetCompletionItems_ReturnsEmptyCompletionItemWithSpaceAfterStartTagAndAttribute()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("@{< te=\"\"");
            var location = new SourceSpan(6, 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Empty(completionItems);
        }

        [Fact]
        public void GetCompletionItems_ReturnsEmptyCompletionItemWhenInsideAttributeArea()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("<p < >");
            var location = new SourceSpan(4, 0);

            // Act
            var completionItems = Provider.GetCompletionItems(syntaxTree, null, location);

            // Assert
            Assert.Empty(completionItems);
        }

        private static void AssertRazorCompletionItem(RazorCompletionItem item)
        {
            Assert.Equal(item.DisplayText, SyntaxConstants.TextTagName);
            Assert.Equal(item.InsertText, SyntaxConstants.TextTagName);
            var completionDescription = item.GetMarkupTransitionCompletionDescription();
            Assert.Equal(Resources.MarkupTransition_Description, completionDescription.Description);
        }

        private static RazorSyntaxTree CreateSyntaxTree(string text, params DirectiveDescriptor[] directives)
        {
            return CreateSyntaxTree(text, FileKinds.Legacy, directives);
        }

        private static RazorSyntaxTree CreateSyntaxTree(string text, string fileKind, params DirectiveDescriptor[] directives)
        {
            var sourceDocument = TestRazorSourceDocument.Create(text);
            var options = RazorParserOptions.Create(builder =>
            {
                foreach (var directive in directives)
                {
                    builder.Directives.Add(directive);
                }
            }, fileKind);
            var syntaxTree = RazorSyntaxTree.Parse(sourceDocument, options);
            return syntaxTree;
        }
    }
}

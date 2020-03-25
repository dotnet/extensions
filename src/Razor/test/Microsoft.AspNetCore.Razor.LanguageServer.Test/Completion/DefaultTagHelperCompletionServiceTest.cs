// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.VisualStudio.Editor.Razor;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{

    public class DefaultTagHelperCompletionServiceTest : DefaultTagHelperServiceTestBase
    {
        [Fact]
        public void GetNearestAncestorTagInfo_MarkupElement()
        {
            // Arrange
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<p><strong></strong></p>");
            var sourceSpan = new SourceSpan(33 + Environment.NewLine.Length, 0);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var owner = syntaxTree.Root.LocateOwner(new SourceChange(sourceSpan, string.Empty));
            var element = owner.FirstAncestorOrSelf<MarkupElementSyntax>();
            var service = new DefaultTagHelperFactsService();

            // Act
            var (ancestorName, ancestorIsTagHelper) = service.GetNearestAncestorTagInfo(element.Ancestors());

            // Assert
            Assert.Equal("p", ancestorName);
            Assert.False(ancestorIsTagHelper);
        }

        [Fact]
        public void GetNearestAncestorTagInfo_TagHelperElement()
        {
            // Arrange
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test1><test2></test2></test1>", DefaultTagHelpers);
            var sourceSpan = new SourceSpan(37 + Environment.NewLine.Length, 0);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var owner = syntaxTree.Root.LocateOwner(new SourceChange(sourceSpan, string.Empty));
            var element = owner.FirstAncestorOrSelf<MarkupTagHelperElementSyntax>();
            var service = new DefaultTagHelperFactsService();

            // Act
            var (ancestorName, ancestorIsTagHelper) = service.GetNearestAncestorTagInfo(element.Ancestors());

            // Assert
            Assert.Equal("test1", ancestorName);
            Assert.True(ancestorIsTagHelper);
        }

        [Fact]
        public void GetCompletionAt_AtEmptyTagName_ReturnsCompletions()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService, HtmlFactsService, TagHelperFactsService);
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<", DefaultTagHelpers);
            var sourceSpan = new SourceSpan(30 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Collection(
                completions,
                completion => Assert.Equal("test1", completion.InsertText),
                completion => Assert.Equal("test2", completion.InsertText));
        }

        [Fact]
        public void GetCompletionAt_OutsideOfTagName_DoesNotReturnCompletions()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService, HtmlFactsService, TagHelperFactsService);
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<br />", DefaultTagHelpers);
            var sourceSpan = new SourceSpan(33 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Empty(completions);
        }

        [Fact]
        public void GetCompletionAt_AtHtmlElementNameEdge_ReturnsCompletions()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService, HtmlFactsService, TagHelperFactsService);
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<br />", DefaultTagHelpers);
            var sourceSpan = new SourceSpan(32 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Collection(
                completions,
                completion => Assert.Equal("test1", completion.InsertText),
                completion => Assert.Equal("test2", completion.InsertText));
        }

        [Fact]
        public void GetCompletionAt_AtTagHelperElementNameEdge_ReturnsCompletions()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService, HtmlFactsService, TagHelperFactsService);
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 />", DefaultTagHelpers);
            var sourceSpan = new SourceSpan(35 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Collection(
                completions,
                completion => Assert.Equal("test1", completion.InsertText),
                completion => Assert.Equal("test2", completion.InsertText));
        }

        [Fact]
        public void GetCompletionAt_AtAttributeEdge_IntAttribute_ReturnsCompletionsWithSnippet()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService, HtmlFactsService, TagHelperFactsService);
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 />", DefaultTagHelpers);
            var sourceSpan = new SourceSpan(36 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Collection(
                completions,
                completion =>
                {
                    Assert.Equal("bool-val", completion.InsertText);
                    Assert.Equal(InsertTextFormat.PlainText, completion.InsertTextFormat);
                },
                completion =>
                {
                    Assert.Equal("int-val=\"$1\"", completion.InsertText);
                    Assert.Equal(InsertTextFormat.Snippet, completion.InsertTextFormat);
                });
        }

        [Fact]
        public void GetCompletionAt_AtAttributeEdge_BoolAttribute_ReturnsCompletionsWithoutSnippet()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService, HtmlFactsService, TagHelperFactsService);
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test2 />", DefaultTagHelpers);
            var sourceSpan = new SourceSpan(36 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Collection(
                completions,
                completion =>
                {
                    Assert.Equal("bool-val", completion.InsertText);
                    Assert.Equal(InsertTextFormat.PlainText, completion.InsertTextFormat);
                },
                completion =>
                {
                    Assert.Equal("int-val=\"$1\"", completion.InsertText);
                    Assert.Equal(InsertTextFormat.Snippet, completion.InsertTextFormat);
                });
        }

        [Fact]
        public void GetCompletionAt_AtAttributeEdge_IndexerBoolAttribute_ReturnsCompletionsWithAndWithoutSnippet()
        {
            // Arrange
            var tagHelper = TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly");
            tagHelper.TagMatchingRule(rule => rule.TagName = "test");
            tagHelper.SetTypeName("TestTagHelper");
            tagHelper.BindAttribute(attribute =>
            {
                attribute.Name = "bool-val";
                attribute.SetPropertyName("BoolVal");
                attribute.TypeName = ("System.Collections.Generic.IDictionary<System.String, System.Boolean>");
                attribute.AsDictionary("bool-val-", typeof(bool).FullName);
            });
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService, HtmlFactsService, TagHelperFactsService);
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test />", tagHelper.Build());
            var sourceSpan = new SourceSpan(35 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Collection(
                completions,
                completion =>
                {
                    Assert.Equal("bool-val=\"$1\"", completion.InsertText);
                    Assert.Equal(InsertTextFormat.Snippet, completion.InsertTextFormat);
                },
                completion =>
                {
                    Assert.Equal("bool-val-", completion.InsertText);
                    Assert.Equal(InsertTextFormat.PlainText, completion.InsertTextFormat);
                });
        }

        [Fact]
        public void GetCompletionAt_AtAttributeEdge_IndexerAttribute_ReturnsCompletionsWithSnippet()
        {
            // Arrange
            var tagHelper = TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly");
            tagHelper.TagMatchingRule(rule => rule.TagName = "test");
            tagHelper.SetTypeName("TestTagHelper");
            tagHelper.BindAttribute(attribute =>
            {
                attribute.Name = "int-val";
                attribute.SetPropertyName("IntVal");
                attribute.TypeName = ("System.Collections.Generic.IDictionary<System.String, System.Int32>");
                attribute.AsDictionary("int-val-", typeof(int).FullName);
            });
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService, HtmlFactsService, TagHelperFactsService);
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test />", tagHelper.Build());
            var sourceSpan = new SourceSpan(35 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Collection(
                completions,
                completion =>
                {
                    Assert.Equal("int-val=\"$1\"", completion.InsertText);
                    Assert.Equal(InsertTextFormat.Snippet, completion.InsertTextFormat);
                },
                completion =>
                {
                    Assert.Equal("int-val-$1=\"$2\"", completion.InsertText);
                    Assert.Equal(InsertTextFormat.Snippet, completion.InsertTextFormat);
                });
        }

        [Fact]
        public void GetCompletionAt_MinimizedAttribute_ReturnsCompletions()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService, HtmlFactsService, TagHelperFactsService);
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test2 unbound />", DefaultTagHelpers);
            var sourceSpan = new SourceSpan(38 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Collection(
                completions,
                completion => Assert.Equal("bool-val", completion.FilterText),
                completion => Assert.Equal("int-val", completion.FilterText));
        }

        [Fact]
        public void GetCompletionAt_MinimizedTagHelperAttribute_ReturnsCompletions()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService, HtmlFactsService, TagHelperFactsService);
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test2 bool-val />", DefaultTagHelpers);
            var sourceSpan = new SourceSpan(38 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Collection(
                completions,
                completion => Assert.Equal("bool-val", completion.FilterText),
                completion => Assert.Equal("int-val", completion.FilterText));
        }

        [Fact]
        public void GetCompletionAt_HtmlAttribute_ReturnsCompletions()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService, HtmlFactsService, TagHelperFactsService);
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test2 class='' />", DefaultTagHelpers);
            var sourceSpan = new SourceSpan(38 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Collection(
                completions,
                completion => Assert.Equal("bool-val", completion.FilterText),
                completion => Assert.Equal("int-val", completion.FilterText));
        }

        [Fact]
        public void GetCompletionAt_TagHelperAttribute_ReturnsCompletions()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService, HtmlFactsService, TagHelperFactsService);
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test2 int-val='123' />", DefaultTagHelpers);
            var sourceSpan = new SourceSpan(38 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Collection(
                completions,
                completion => Assert.Equal("bool-val", completion.FilterText),
                completion => Assert.Equal("int-val", completion.FilterText));
        }

        [Fact]
        public void GetCompletionsAt_MalformedAttributeValue_ReturnsCompletions()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService, HtmlFactsService, TagHelperFactsService);
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test2 int-val='>";
            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);
            var sourceSpan = new SourceSpan(38 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Collection(
                completions,
                completion => Assert.Equal("bool-val", completion.FilterText),
                completion => Assert.Equal("int-val", completion.FilterText));
        }

        [Fact]
        public void GetCompletionsAt_MalformedAttributeName_ReturnsCompletions()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService, HtmlFactsService, TagHelperFactsService);
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test2 int->", DefaultTagHelpers);
            var sourceSpan = new SourceSpan(38 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Collection(
                completions,
                completion => Assert.Equal("bool-val", completion.FilterText),
                completion => Assert.Equal("int-val", completion.FilterText));
        }

        [Fact]
        public void GetCompletionAt_HtmlAttributeValue_DoesNotReturnCompletions()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService, HtmlFactsService, TagHelperFactsService);
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test2 class='' />", DefaultTagHelpers);
            var sourceSpan = new SourceSpan(43 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Empty(completions);
        }

        [Fact]
        public void GetCompletionsAt_AttributePrefix_ReturnsCompletions()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService, HtmlFactsService, TagHelperFactsService);
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test2        class=''>";
            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);
            var sourceSpan = new SourceSpan(38 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Collection(
                completions,
                completion => Assert.Equal("bool-val", completion.FilterText),
                completion => Assert.Equal("int-val", completion.FilterText));
        }
    }
}

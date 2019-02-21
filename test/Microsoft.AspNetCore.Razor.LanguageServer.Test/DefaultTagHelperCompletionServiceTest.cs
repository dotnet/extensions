// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Test.Infrastructure;
using Microsoft.VisualStudio.Editor.Razor;
using Xunit;
using DefaultRazorTagHelperCompletionService = Microsoft.VisualStudio.Editor.Razor.DefaultTagHelperCompletionService;
using RazorTagHelperCompletionService = Microsoft.VisualStudio.Editor.Razor.TagHelperCompletionService;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class DefaultTagHelperCompletionServiceTest : TestBase
    {
        public DefaultTagHelperCompletionServiceTest()
        {
            var builder1 = TagHelperDescriptorBuilder.Create("Test1TagHelper", "TestAssembly");
            builder1.TagMatchingRule(rule => rule.TagName = "test1");
            builder1.SetTypeName("Test1TagHelper");

            var builder2 = TagHelperDescriptorBuilder.Create("Test2TagHelper", "TestAssembly");
            builder2.TagMatchingRule(rule => rule.TagName = "test2");
            builder2.SetTypeName("Test2TagHelper");

            DefaultTagHelpers = new[] { builder1.Build(), builder2.Build() };
            var tagHelperFactsService = new DefaultTagHelperFactsService();
            RazorTagHelperCompletionService = new DefaultRazorTagHelperCompletionService(tagHelperFactsService);
        }

        private TagHelperDescriptor[] DefaultTagHelpers { get; }

        private RazorTagHelperCompletionService RazorTagHelperCompletionService { get; }

        [Fact]
        public void GetNearestAncestorTagInfo_MarkupElement()
        {
            // Arrange
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<p><strong></strong></p>");
            var sourceSpan = new SourceSpan(33 + Environment.NewLine.Length, 0);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var owner = syntaxTree.Root.LocateOwner(new SourceChange(sourceSpan, string.Empty));
            var element = owner.FirstAncestorOrSelf<MarkupElementSyntax>();

            // Act
            var (ancestorName, ancestorIsTagHelper) = DefaultTagHelperCompletionService.GetNearestAncestorTagInfo(element.Ancestors());

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

            // Act
            var (ancestorName, ancestorIsTagHelper) = DefaultTagHelperCompletionService.GetNearestAncestorTagInfo(element.Ancestors());

            // Assert
            Assert.Equal("test1", ancestorName);
            Assert.True(ancestorIsTagHelper);
        }

        [Fact]
        public void StringifyAttributes_TagHelperAttribute()
        {
            // Arrange
            var tagHelper = TagHelperDescriptorBuilder.Create("WithBoundAttribute", "TestAssembly");
            tagHelper.TagMatchingRule(rule => rule.TagName = "test");
            tagHelper.BindAttribute(attribute =>
            {
                attribute.Name = "bound";
                attribute.SetPropertyName("Bound");
                attribute.TypeName = typeof(bool).FullName;
            });
            tagHelper.SetTypeName("WithBoundAttribute");
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test bound='true' />", tagHelper.Build());
            var syntaxTree = codeDocument.GetSyntaxTree();
            var sourceSpan = new SourceSpan(30 + Environment.NewLine.Length, 0);
            var sourceChangeLocation = new SourceChange(sourceSpan, string.Empty);
            var startTag = (MarkupTagHelperStartTagSyntax)syntaxTree.Root.LocateOwner(sourceChangeLocation).Parent;

            // Act
            var attributes = DefaultTagHelperCompletionService.StringifyAttributes(startTag.Attributes);

            // Assert
            Assert.Collection(
                attributes,
                attribute =>
                {
                    Assert.Equal("bound", attribute.Key);
                    Assert.Equal("true", attribute.Value);
                });
        }

        [Fact]
        public void StringifyAttributes_MinimizedTagHelperAttribute()
        {
            // Arrange
            var tagHelper = TagHelperDescriptorBuilder.Create("WithBoundAttribute", "TestAssembly");
            tagHelper.TagMatchingRule(rule => rule.TagName = "test");
            tagHelper.BindAttribute(attribute =>
            {
                attribute.Name = "bound";
                attribute.SetPropertyName("Bound");
                attribute.TypeName = typeof(bool).FullName;
            });
            tagHelper.SetTypeName("WithBoundAttribute");
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test bound />", tagHelper.Build());
            var syntaxTree = codeDocument.GetSyntaxTree();
            var sourceSpan = new SourceSpan(30 + Environment.NewLine.Length, 0);
            var sourceChangeLocation = new SourceChange(sourceSpan, string.Empty);
            var startTag = (MarkupTagHelperStartTagSyntax)syntaxTree.Root.LocateOwner(sourceChangeLocation).Parent;

            // Act
            var attributes = DefaultTagHelperCompletionService.StringifyAttributes(startTag.Attributes);

            // Assert
            Assert.Collection(
                attributes,
                attribute =>
                {
                    Assert.Equal("bound", attribute.Key);
                    Assert.Equal(string.Empty, attribute.Value);
                });
        }

        [Fact]
        public void StringifyAttributes_UnboundAttribute()
        {
            // Arrange
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<input unbound='hello world' />", DefaultTagHelpers);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var sourceSpan = new SourceSpan(30 + Environment.NewLine.Length, 0);
            var sourceChangeLocation = new SourceChange(sourceSpan, string.Empty);
            var startTag = (MarkupStartTagSyntax)syntaxTree.Root.LocateOwner(sourceChangeLocation).Parent;

            // Act
            var attributes = DefaultTagHelperCompletionService.StringifyAttributes(startTag.Attributes);

            // Assert
            Assert.Collection(
                attributes,
                attribute =>
                {
                    Assert.Equal("unbound", attribute.Key);
                    Assert.Equal("hello world", attribute.Value);
                });
        }

        [Fact]
        public void StringifyAttributes_UnboundMinimizedAttribute()
        {
            // Arrange
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<input unbound />", DefaultTagHelpers);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var sourceSpan = new SourceSpan(30 + Environment.NewLine.Length, 0);
            var sourceChangeLocation = new SourceChange(sourceSpan, string.Empty);
            var startTag = (MarkupStartTagSyntax)syntaxTree.Root.LocateOwner(sourceChangeLocation).Parent;

            // Act
            var attributes = DefaultTagHelperCompletionService.StringifyAttributes(startTag.Attributes);

            // Assert
            Assert.Collection(
                attributes,
                attribute =>
                {
                    Assert.Equal("unbound", attribute.Key);
                    Assert.Equal(string.Empty, attribute.Value);
                });
        }

        [Fact]
        public void StringifyAttributes_IgnoresMiscContent()
        {
            // Arrange
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<input unbound @DateTime.Now />", DefaultTagHelpers);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var sourceSpan = new SourceSpan(30 + Environment.NewLine.Length, 0);
            var sourceChangeLocation = new SourceChange(sourceSpan, string.Empty);
            var startTag = (MarkupStartTagSyntax)syntaxTree.Root.LocateOwner(sourceChangeLocation).Parent;

            // Act
            var attributes = DefaultTagHelperCompletionService.StringifyAttributes(startTag.Attributes);

            // Assert
            Assert.Collection(
                attributes,
                attribute =>
                {
                    Assert.Equal("unbound", attribute.Key);
                    Assert.Equal(string.Empty, attribute.Value);
                });
        }

        [Fact]
        public void GetCompletionAt_AtEmptyTagName_ReturnsCompletions()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService);
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
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService);
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
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService);
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
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService);
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

        private static RazorCodeDocument CreateCodeDocument(string text, params TagHelperDescriptor[] tagHelpers)
        {
            tagHelpers = tagHelpers ?? Array.Empty<TagHelperDescriptor>();
            var sourceDocument = TestRazorSourceDocument.Create(text);
            var projectEngine = RazorProjectEngine.Create(builder => { });
            var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, "mvc", Array.Empty<RazorSourceDocument>(), tagHelpers);
            return codeDocument;
        }
    }
}

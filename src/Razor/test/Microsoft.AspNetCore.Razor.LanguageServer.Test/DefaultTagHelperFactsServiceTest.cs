// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion;
using Microsoft.VisualStudio.Editor.Razor;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test
{
    public class DefaultTagHelperFactsServiceTest : DefaultTagHelperServiceTestBase
    {
        [Fact]
        public void StringifyAttributes_DirectiveAttribute()
        {
            // Arrange
            var codeDocument = CreateComponentDocument($"<TestElement @test='abc' />", DefaultTagHelpers);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var sourceSpan = new SourceSpan(3, 0);
            var sourceChangeLocation = new SourceChange(sourceSpan, string.Empty);
            var startTag = (MarkupTagHelperStartTagSyntax)syntaxTree.Root.LocateOwner(sourceChangeLocation).Parent;

            // Act
            var attributes = TagHelperFactsService.StringifyAttributes(startTag.Attributes);

            // Assert
            Assert.Collection(
                attributes,
                attribute =>
                {
                    Assert.Equal("@test", attribute.Key);
                    Assert.Equal("abc", attribute.Value);
                });
        }

        [Fact]
        public void StringifyAttributes_DirectiveAttributeWithParameter()
        {
            // Arrange
            var codeDocument = CreateComponentDocument($"<TestElement @test:something='abc' />", DefaultTagHelpers);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var sourceSpan = new SourceSpan(3, 0);
            var sourceChangeLocation = new SourceChange(sourceSpan, string.Empty);
            var startTag = (MarkupTagHelperStartTagSyntax)syntaxTree.Root.LocateOwner(sourceChangeLocation).Parent;

            // Act
            var attributes = TagHelperFactsService.StringifyAttributes(startTag.Attributes);

            // Assert
            Assert.Collection(
                attributes,
                attribute =>
                {
                    Assert.Equal("@test:something", attribute.Key);
                    Assert.Equal("abc", attribute.Value);
                });
        }

        [Fact]
        public void StringifyAttributes_MinimizedDirectiveAttribute()
        {
            // Arrange
            var codeDocument = CreateComponentDocument($"<TestElement @minimized />", DefaultTagHelpers);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var sourceSpan = new SourceSpan(3, 0);
            var sourceChangeLocation = new SourceChange(sourceSpan, string.Empty);
            var startTag = (MarkupTagHelperStartTagSyntax)syntaxTree.Root.LocateOwner(sourceChangeLocation).Parent;

            // Act
            var attributes = TagHelperFactsService.StringifyAttributes(startTag.Attributes);

            // Assert
            Assert.Collection(
                attributes,
                attribute =>
                {
                    Assert.Equal("@minimized", attribute.Key);
                    Assert.Equal(string.Empty, attribute.Value);
                });
        }

        [Fact]
        public void StringifyAttributes_MinimizedDirectiveAttributeWithParameter()
        {
            // Arrange
            var codeDocument = CreateComponentDocument($"<TestElement @minimized:something />", DefaultTagHelpers);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var sourceSpan = new SourceSpan(3, 0);
            var sourceChangeLocation = new SourceChange(sourceSpan, string.Empty);
            var startTag = (MarkupTagHelperStartTagSyntax)syntaxTree.Root.LocateOwner(sourceChangeLocation).Parent;

            // Act
            var attributes = TagHelperFactsService.StringifyAttributes(startTag.Attributes);

            // Assert
            Assert.Collection(
                attributes,
                attribute =>
                {
                    Assert.Equal("@minimized:something", attribute.Key);
                    Assert.Equal(string.Empty, attribute.Value);
                });
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
            var codeDocument = DefaultTagHelperCompletionServiceTest.CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test bound='true' />", tagHelper.Build());
            var syntaxTree = codeDocument.GetSyntaxTree();
            var sourceSpan = new SourceSpan(30 + Environment.NewLine.Length, 0);
            var sourceChangeLocation = new SourceChange(sourceSpan, string.Empty);
            var startTag = (MarkupTagHelperStartTagSyntax)syntaxTree.Root.LocateOwner(sourceChangeLocation).Parent;

            // Act
            var attributes = TagHelperFactsService.StringifyAttributes(startTag.Attributes);

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
            var codeDocument = DefaultTagHelperCompletionServiceTest.CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test bound />", tagHelper.Build());
            var syntaxTree = codeDocument.GetSyntaxTree();
            var sourceSpan = new SourceSpan(30 + Environment.NewLine.Length, 0);
            var sourceChangeLocation = new SourceChange(sourceSpan, string.Empty);
            var startTag = (MarkupTagHelperStartTagSyntax)syntaxTree.Root.LocateOwner(sourceChangeLocation).Parent;

            // Act
            var attributes = TagHelperFactsService.StringifyAttributes(startTag.Attributes);

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
            var codeDocument = DefaultTagHelperCompletionServiceTest.CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<input unbound='hello world' />", DefaultTagHelpers);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var sourceSpan = new SourceSpan(30 + Environment.NewLine.Length, 0);
            var sourceChangeLocation = new SourceChange(sourceSpan, string.Empty);
            var startTag = (MarkupStartTagSyntax)syntaxTree.Root.LocateOwner(sourceChangeLocation).Parent;

            // Act
            var attributes = TagHelperFactsService.StringifyAttributes(startTag.Attributes);

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
            var codeDocument = DefaultTagHelperCompletionServiceTest.CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<input unbound />", DefaultTagHelpers);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var sourceSpan = new SourceSpan(30 + Environment.NewLine.Length, 0);
            var sourceChangeLocation = new SourceChange(sourceSpan, string.Empty);
            var startTag = (MarkupStartTagSyntax)syntaxTree.Root.LocateOwner(sourceChangeLocation).Parent;

            // Act
            var attributes = TagHelperFactsService.StringifyAttributes(startTag.Attributes);

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
            var codeDocument = DefaultTagHelperCompletionServiceTest.CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<input unbound @DateTime.Now />", DefaultTagHelpers);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var sourceSpan = new SourceSpan(30 + Environment.NewLine.Length, 0);
            var sourceChangeLocation = new SourceChange(sourceSpan, string.Empty);
            var startTag = (MarkupStartTagSyntax)syntaxTree.Root.LocateOwner(sourceChangeLocation).Parent;

            // Act
            var attributes = TagHelperFactsService.StringifyAttributes(startTag.Attributes);

            // Assert
            Assert.Collection(
                attributes,
                attribute =>
                {
                    Assert.Equal("unbound", attribute.Key);
                    Assert.Equal(string.Empty, attribute.Value);
                });
        }

        private static RazorCodeDocument CreateComponentDocument(string text, params TagHelperDescriptor[] tagHelpers)
        {
            tagHelpers = tagHelpers ?? Array.Empty<TagHelperDescriptor>();
            var sourceDocument = TestRazorSourceDocument.Create(text);
            var projectEngine = RazorProjectEngine.Create(builder => { });
            var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, FileKinds.Component, Array.Empty<RazorSourceDocument>(), tagHelpers);
            return codeDocument;
        }
    }
}

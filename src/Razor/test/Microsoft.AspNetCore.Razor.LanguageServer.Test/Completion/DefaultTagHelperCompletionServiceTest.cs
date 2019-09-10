// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.Editor.Razor;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Microsoft.AspNetCore.Razor.Language.Components;
using Xunit;
using DefaultRazorTagHelperCompletionService = Microsoft.VisualStudio.Editor.Razor.DefaultTagHelperCompletionService;
using RazorTagHelperCompletionService = Microsoft.VisualStudio.Editor.Razor.TagHelperCompletionService;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    public class DefaultTagHelperCompletionServiceTest : LanguageServerTestBase
    {
        public DefaultTagHelperCompletionServiceTest()
        {
            var builder1 = TagHelperDescriptorBuilder.Create("Test1TagHelper", "TestAssembly");
            builder1.TagMatchingRule(rule => rule.TagName = "test1");
            builder1.SetTypeName("Test1TagHelper");
            builder1.BindAttribute(attribute =>
            {
                attribute.Name = "bool-val";
                attribute.SetPropertyName("BoolVal");
                attribute.TypeName = typeof(bool).FullName;
            });
            builder1.BindAttribute(attribute =>
            {
                attribute.Name = "int-val";
                attribute.SetPropertyName("IntVal");
                attribute.TypeName = typeof(int).FullName;
            });

            var builder2 = TagHelperDescriptorBuilder.Create("Test2TagHelper", "TestAssembly");
            builder2.TagMatchingRule(rule => rule.TagName = "test2");
            builder2.SetTypeName("Test2TagHelper");
            builder2.BindAttribute(attribute =>
            {
                attribute.Name = "bool-val";
                attribute.SetPropertyName("BoolVal");
                attribute.TypeName = typeof(bool).FullName;
            });
            builder2.BindAttribute(attribute =>
            {
                attribute.Name = "int-val";
                attribute.SetPropertyName("IntVal");
                attribute.TypeName = typeof(int).FullName;
            });

            var directiveAttribute1 = TagHelperDescriptorBuilder.Create(ComponentMetadata.Component.TagHelperKind, "TestDirectiveAttribute", "TestAssembly");
            directiveAttribute1.TagMatchingRule(rule =>
            {
                rule.TagName = "*";
            });
            directiveAttribute1.BindAttribute(attribute =>
            {
                attribute.Metadata[ComponentMetadata.Common.DirectiveAttribute] = bool.TrueString;
                attribute.Name = "@test";
                attribute.SetPropertyName("Test");
                attribute.TypeName = typeof(string).FullName;

                attribute.BindAttributeParameter(parameter =>
                {
                    parameter.Name = "something";
                    parameter.TypeName = typeof(string).FullName;

                    parameter.SetPropertyName("Something");
                });
            });
            directiveAttribute1.Metadata[ComponentMetadata.Component.NameMatchKey] = ComponentMetadata.Component.FullyQualifiedNameMatch;
            directiveAttribute1.SetTypeName("TestDirectiveAttribute");

            var directiveAttribute2 = TagHelperDescriptorBuilder.Create(ComponentMetadata.Component.TagHelperKind, "MinimizedDirectiveAttribute", "TestAssembly");
            directiveAttribute2.TagMatchingRule(rule =>
            {
                rule.TagName = "*";
            });
            directiveAttribute2.BindAttribute(attribute =>
            {
                attribute.Metadata[ComponentMetadata.Common.DirectiveAttribute] = bool.TrueString;
                attribute.Name = "@minimized";
                attribute.SetPropertyName("Minimized");
                attribute.TypeName = typeof(bool).FullName;

                attribute.BindAttributeParameter(parameter =>
                {
                    parameter.Name = "something";
                    parameter.TypeName = typeof(string).FullName;

                    parameter.SetPropertyName("Something");
                });
            });
            directiveAttribute2.Metadata[ComponentMetadata.Component.NameMatchKey] = ComponentMetadata.Component.FullyQualifiedNameMatch;
            directiveAttribute2.SetTypeName("TestDirectiveAttribute");

            DefaultTagHelpers = new[] { builder1.Build(), builder2.Build(), directiveAttribute1.Build() };
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
        public void StringifyAttributes_DirectiveAttribute()
        {
            // Arrange
            var codeDocument = CreateComponentDocument($"<TestElement @test='abc' />", DefaultTagHelpers);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var sourceSpan = new SourceSpan(3, 0);
            var sourceChangeLocation = new SourceChange(sourceSpan, string.Empty);
            var startTag = (MarkupTagHelperStartTagSyntax)syntaxTree.Root.LocateOwner(sourceChangeLocation).Parent;

            // Act
            var attributes = DefaultTagHelperCompletionService.StringifyAttributes(startTag.Attributes);

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
            var attributes = DefaultTagHelperCompletionService.StringifyAttributes(startTag.Attributes);

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
            var attributes = DefaultTagHelperCompletionService.StringifyAttributes(startTag.Attributes);

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
            var attributes = DefaultTagHelperCompletionService.StringifyAttributes(startTag.Attributes);

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

        [Fact]
        public void GetCompletionAt_AtAttributeEdge_IntAttribute_ReturnsCompletionsWithSnippet()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService);
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
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService);
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
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService);
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
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService);
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
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService);
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
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService);
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test2 bool-val />", DefaultTagHelpers);
            var sourceSpan = new SourceSpan(38 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Collection(
                completions,
                completion => Assert.Equal("int-val", completion.FilterText));
        }

        [Fact]
        public void GetCompletionAt_HtmlAttribute_ReturnsCompletions()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService);
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
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService);
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test2 int-val='123' />", DefaultTagHelpers);
            var sourceSpan = new SourceSpan(38 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Collection(
                completions,
                completion => Assert.Equal("bool-val", completion.FilterText));
        }

        [Fact]
        public void GetCompletionsAt_MalformedAttributeValue_ReturnsCompletions()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService);
            var codeDocument = CreateCodeDocument($"@addTagHelper *, TestAssembly{Environment.NewLine}<test2 int-val='>", DefaultTagHelpers);
            var sourceSpan = new SourceSpan(38 + Environment.NewLine.Length, 0);

            // Act
            var completions = service.GetCompletionsAt(sourceSpan, codeDocument);

            // Assert
            Assert.Collection(
                completions,
                completion => Assert.Equal("bool-val", completion.FilterText));
        }

        [Fact]
        public void GetCompletionsAt_MalformedAttributeName_ReturnsCompletions()
        {
            // Arrange
            var service = new DefaultTagHelperCompletionService(RazorTagHelperCompletionService);
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

        private static RazorCodeDocument CreateCodeDocument(string text, params TagHelperDescriptor[] tagHelpers)
        {
            tagHelpers = tagHelpers ?? Array.Empty<TagHelperDescriptor>();
            var sourceDocument = TestRazorSourceDocument.Create(text);
            var projectEngine = RazorProjectEngine.Create(builder => { });
            var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, "mvc", Array.Empty<RazorSourceDocument>(), tagHelpers);
            return codeDocument;
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

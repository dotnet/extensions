// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion;
using Microsoft.AspNetCore.Razor.LanguageServer.Hover;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;
using RangeModel = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Hover
{
    public class DefaultRazorHoverInfoServiceTest : DefaultTagHelperServiceTestBase
    {
        [Fact]
        public void GetHoverInfo_TagHelper_Element()
        {
            // Arrange
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>";
            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);
            var service = GetDefaultRazorHoverInfoService();
            var location = new SourceLocation(txt.IndexOf("test1"), -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Contains("**Test1TagHelper**", hover.Contents.MarkupContent.Value);
            var expectedRange = new RangeModel(new Position(1, 1), new Position(1, 6));
            Assert.Equal(expectedRange, hover.Range);
        }

        [Fact]
        public void GetHoverInfo_TagHelper_Attribute()
        {
            // Arrange
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true'></test1>";
            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);
            var service = GetDefaultRazorHoverInfoService();
            var location = new SourceLocation(txt.IndexOf("bool-val"), -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Contains("**BoolVal**", hover.Contents.MarkupContent.Value);
            Assert.DoesNotContain("**IntVal**", hover.Contents.MarkupContent.Value);
            var expectedRange = new RangeModel(new Position(1, 7), new Position(1, 15));
            Assert.Equal(expectedRange, hover.Range);
        }

        [Fact]
        public void GetHoverInfo_TagHelper_AttributeValue_ReturnsNull()
        {
            // Arrange
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true'></test1>";
            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);
            var service = GetDefaultRazorHoverInfoService();
            var location = new SourceLocation(txt.IndexOf("true"), -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Null(hover);
        }

        [Fact]
        public void GetHoverInfo_TagHelper_AfterAttributeEquals_ReturnsNull()
        {
            // Arrange
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true'></test1>";
            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);
            var service = GetDefaultRazorHoverInfoService();
            var location = new SourceLocation(txt.IndexOf("=") + 1, -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Null(hover);
        }

        [Fact]
        public void GetHoverInfo_TagHelper_AttributeEnd_ReturnsNull()
        {
            // Arrange
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val='true'></test1>";
            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);
            var service = GetDefaultRazorHoverInfoService();
            var location = new SourceLocation(txt.IndexOf("true'") + 5, -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Null(hover);
        }

        [Fact]
        public void GetHoverInfo_TagHelper_MinimizedAttribute()
        {
            // Arrange
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val></test1>";
            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);
            var service = GetDefaultRazorHoverInfoService();
            var location = new SourceLocation(txt.IndexOf("bool-val"), -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Contains("**BoolVal**", hover.Contents.MarkupContent.Value);
            Assert.DoesNotContain("**IntVal**", hover.Contents.MarkupContent.Value);
            var expectedRange = new RangeModel(new Position(1, 7), new Position(1, 15));
            Assert.Equal(expectedRange, hover.Range);
        }

        [Fact]
        public void GetHoverInfo_DirectiveAttribute_HasResult()
        {
            // Arrange
            var txt = @"@addTagHelper *, TestAssembly
<any @test=""Increment"" />
@code{
    public void Increment(){
    }
}";
            var codeDocument = CreateCodeDocument(txt, "text.razor", DefaultTagHelpers);
            var service = GetDefaultRazorHoverInfoService();
            var charIndex = txt.IndexOf("@test") + 2;
            var location = new SourceLocation(charIndex, -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.NotNull(hover);
            Assert.Contains("**Test**", hover.Contents.MarkupContent.Value);
            var expectedRange = new RangeModel(new Position(1, 4), new Position(1, 22));
            Assert.Equal(expectedRange, hover.Range);
        }

        [Fact]
        public void GetHoverInfo_TagHelper_MalformedElement()
        {
            // Arrange
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1<hello";
            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);
            var service = GetDefaultRazorHoverInfoService();
            var location = new SourceLocation(txt.IndexOf("test1"), -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Contains("**Test1TagHelper**", hover.Contents.MarkupContent.Value);
            var expectedRange = new RangeModel(new Position(1, 1), new Position(1, 6));
            Assert.Equal(expectedRange, hover.Range);
        }

        [Fact]
        public void GetHoverInfo_TagHelper_MalformedAttribute()
        {
            // Arrange
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val=\"aslj alsk<strong>";
            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);
            var service = GetDefaultRazorHoverInfoService();
            var location = new SourceLocation(txt.IndexOf("bool-val"), -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Contains("**BoolVal**", hover.Contents.MarkupContent.Value);
            Assert.DoesNotContain("**IntVal**", hover.Contents.MarkupContent.Value);
            var expectedRange = new RangeModel(new Position(1, 7), new Position(1, 15));
            Assert.Equal(expectedRange, hover.Range);
        }

        [Fact]
        public void GetHoverInfo_HTML_MarkupElement()
        {
            // Arrange
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<p><strong></strong></p>";
            var codeDocument = CreateCodeDocument(txt);
            var service = GetDefaultRazorHoverInfoService();
            var location = new SourceLocation(txt.IndexOf("strong"), -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Null(hover);
        }

        private DefaultRazorHoverInfoService GetDefaultRazorHoverInfoService()
        {
            var tagHelperDescriptionFactory = new DefaultTagHelperDescriptionFactory();
            return new DefaultRazorHoverInfoService(TagHelperFactsService, tagHelperDescriptionFactory, HtmlFactsService);
        }
    }
}

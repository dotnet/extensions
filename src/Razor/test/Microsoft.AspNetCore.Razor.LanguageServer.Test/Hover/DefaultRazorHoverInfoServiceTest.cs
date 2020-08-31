// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion;
using Microsoft.AspNetCore.Razor.LanguageServer.Hover;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Xunit;
using RangeModel = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Hover
{
    public class DefaultRazorHoverInfoServiceTest : DefaultTagHelperServiceTestBase
    {
        protected ILanguageServer LanguageServer
        {
            get
            {
                var initializeParams = new InitializeParams
                {
                    Capabilities = new ClientCapabilities
                    {
                        TextDocument = new TextDocumentClientCapabilities
                        {
                            Hover = new Supports<HoverCapability>
                            {
                                Value = new HoverCapability
                                {
                                    ContentFormat = new Container<MarkupKind>(MarkupKind.Markdown)
                                }
                            }
                        }
                    }
                };

                var languageServer = new Mock<ILanguageServer>();
                languageServer.SetupGet(server => server.ClientSettings)
                    .Returns(initializeParams);

                return languageServer.Object;
            }
        }

        [Fact]
        public void GetHoverInfo_TagHelper_Element()
        {
            // Arrange
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>";
            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);
            var service = GetDefaultRazorHoverInfoService();
            var location = new SourceLocation(txt.IndexOf("test1", StringComparison.Ordinal), -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Contains("**Test1TagHelper**", hover.Contents.MarkupContent.Value, StringComparison.Ordinal);
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
            var location = new SourceLocation(txt.IndexOf("bool-val", StringComparison.Ordinal), -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Contains("**BoolVal**", hover.Contents.MarkupContent.Value, StringComparison.Ordinal);
            Assert.DoesNotContain("**IntVal**", hover.Contents.MarkupContent.Value, StringComparison.Ordinal);
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
            var location = new SourceLocation(txt.IndexOf("true", StringComparison.Ordinal), -1, -1);

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
            var location = new SourceLocation(txt.IndexOf("=", StringComparison.Ordinal) + 1, -1, -1);

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
            var location = new SourceLocation(txt.IndexOf("true'", StringComparison.Ordinal) + 5, -1, -1);

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
            var location = new SourceLocation(txt.IndexOf("bool-val", StringComparison.Ordinal), -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Contains("**BoolVal**", hover.Contents.MarkupContent.Value, StringComparison.Ordinal);
            Assert.DoesNotContain("**IntVal**", hover.Contents.MarkupContent.Value, StringComparison.Ordinal);
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
            var charIndex = txt.IndexOf("@test", StringComparison.Ordinal) + 2;
            var location = new SourceLocation(charIndex, -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.NotNull(hover);
            Assert.Contains("**Test**", hover.Contents.MarkupContent.Value, StringComparison.Ordinal);
            var expectedRange = new RangeModel(new Position(1, 5), new Position(1, 10));
            Assert.Equal(expectedRange, hover.Range);
        }

        [Fact]
        public void GetHoverInfo_TagHelper_MalformedElement()
        {
            // Arrange
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1<hello";
            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);
            var service = GetDefaultRazorHoverInfoService();
            var location = new SourceLocation(txt.IndexOf("test1", StringComparison.Ordinal), -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Contains("**Test1TagHelper**", hover.Contents.MarkupContent.Value, StringComparison.Ordinal);
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
            var location = new SourceLocation(txt.IndexOf("bool-val", StringComparison.Ordinal), -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Contains("**BoolVal**", hover.Contents.MarkupContent.Value, StringComparison.Ordinal);
            Assert.DoesNotContain("**IntVal**", hover.Contents.MarkupContent.Value, StringComparison.Ordinal);
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
            var location = new SourceLocation(txt.IndexOf("strong", StringComparison.Ordinal), -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Null(hover);
        }

        [Fact]
        public void GetHoverInfo_TagHelper_PlainTextElement()
        {
            // Arrange
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>";
            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);

            var languageServer = LanguageServer;
            languageServer.ClientSettings.Capabilities.TextDocument.Hover.Value.ContentFormat = new Container<MarkupKind>(MarkupKind.PlainText);
            var service = GetDefaultRazorHoverInfoService(languageServer);
            var location = new SourceLocation(txt.IndexOf("test1", StringComparison.Ordinal), -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Contains("Test1TagHelper", hover.Contents.MarkupContent.Value, StringComparison.Ordinal);
            Assert.Equal(MarkupKind.PlainText, hover.Contents.MarkupContent.Kind);
            var expectedRange = new RangeModel(new Position(1, 1), new Position(1, 6));
            Assert.Equal(expectedRange, hover.Range);
        }

        [Fact]
        public void GetHoverInfo_TagHelper_PlainTextAttribute()
        {
            // Arrange
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1 bool-val></test1>";
            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);

            var languageServer = LanguageServer;
            languageServer.ClientSettings.Capabilities.TextDocument.Hover.Value.ContentFormat = new Container<MarkupKind>(MarkupKind.PlainText);
            var service = GetDefaultRazorHoverInfoService(languageServer);
            var location = new SourceLocation(txt.IndexOf("bool-val", StringComparison.Ordinal), -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Contains("BoolVal", hover.Contents.MarkupContent.Value, StringComparison.Ordinal);
            Assert.DoesNotContain("IntVal", hover.Contents.MarkupContent.Value, StringComparison.Ordinal);
            Assert.Equal(MarkupKind.PlainText, hover.Contents.MarkupContent.Kind);
            var expectedRange = new RangeModel(new Position(1, 7), new Position(1, 15));
            Assert.Equal(expectedRange, hover.Range);
        }

        [Fact]
        public void GetHoverInfo_HTML_PlainTextElement()
        {
            // Arrange
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<p><strong></strong></p>";
            var codeDocument = CreateCodeDocument(txt);

            var languageServer = LanguageServer;
            languageServer.ClientSettings.Capabilities.TextDocument.Hover.Value.ContentFormat = new Container<MarkupKind>(MarkupKind.PlainText);
            var service = GetDefaultRazorHoverInfoService(languageServer);
            var location = new SourceLocation(txt.IndexOf("strong", StringComparison.Ordinal), -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Null(hover);
        }

        [Fact]
        public void GetHoverInfo_HTML_PlainTextAttribute()
        {
            // Arrange
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<p><strong class=\"weak\"></strong></p>";
            var codeDocument = CreateCodeDocument(txt);

            var languageServer = LanguageServer;
            languageServer.ClientSettings.Capabilities.TextDocument.Hover.Value.ContentFormat = new Container<MarkupKind>(MarkupKind.PlainText);
            var service = GetDefaultRazorHoverInfoService(languageServer);
            var location = new SourceLocation(txt.IndexOf("weak", StringComparison.Ordinal), -1, -1);

            // Act
            var hover = service.GetHoverInfo(codeDocument, location);

            // Assert
            Assert.Null(hover);
        }

        private DefaultRazorHoverInfoService GetDefaultRazorHoverInfoService(ILanguageServer languageServer = null)
        {
            if (languageServer is null)
            {
                languageServer = LanguageServer;
            }

            var lazy = new Lazy<ILanguageServer>(languageServer);
            var tagHelperDescriptionFactory = new DefaultTagHelperDescriptionFactory(lazy);
            return new DefaultRazorHoverInfoService(TagHelperFactsService, tagHelperDescriptionFactory, HtmlFactsService);
        }
    }
}

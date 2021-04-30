// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.Tooltip;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Tooltip
{
    public class DefaultLSPTagHelperTooltipFactoryTest
    {
        internal static ClientNotifierServiceBase LanguageServer
        {
            get
            {
                var initializeParams = new InitializeParams
                {
                    Capabilities = new ClientCapabilities
                    {
                        TextDocument = new TextDocumentClientCapabilities
                        {
                            Completion = new Supports<CompletionCapability>
                            {
                                Value = new CompletionCapability
                                {
                                    CompletionItem = new CompletionItemCapabilityOptions
                                    {
                                        SnippetSupport = true,
                                        DocumentationFormat = new Container<MarkupKind>(MarkupKind.Markdown)
                                    }
                                }
                            }
                        }
                    }
                };

                var languageServer = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);
                languageServer.SetupGet(server => server.ClientSettings)
                    .Returns(initializeParams);

                return languageServer.Object;
            }
        }

        [Fact]
        public void CleanSummaryContent_Markup_ReplacesSeeCrefs()
        {
            // Arrange
            var summary = "Accepts <see cref=\"T:System.Collections.List{System.String}\" />s";

            // Act
            var cleanedSummary = DefaultLSPTagHelperTooltipFactory.CleanSummaryContent(summary);

            // Assert
            Assert.Equal("Accepts `List<System.String>`s", cleanedSummary);
        }

        [Fact]
        public void CleanSummaryContent_Markup_ReplacesSeeAlsoCrefs()
        {
            // Arrange
            var summary = "Accepts <seealso cref=\"T:System.Collections.List{System.String}\" />s";

            // Act
            var cleanedSummary = DefaultLSPTagHelperTooltipFactory.CleanSummaryContent(summary);

            // Assert
            Assert.Equal("Accepts `List<System.String>`s", cleanedSummary);
        }

        [Fact]
        public void CleanSummaryContent_Markup_TrimsSurroundingWhitespace()
        {
            // Arrange
            var summary = @"
            Hello

    World

";

            // Act
            var cleanedSummary = DefaultLSPTagHelperTooltipFactory.CleanSummaryContent(summary);

            // Assert
            Assert.Equal(@"Hello

World", cleanedSummary);
        }

        [Fact]
        public void TryCreateTooltip_Markup_NoAssociatedTagHelperDescriptions_ReturnsFalse()
        {
            // Arrange
            var descriptionFactory = new DefaultLSPTagHelperTooltipFactory(LanguageServer);
            var elementDescription = AggregateBoundElementDescription.Default;

            // Act
            var result = descriptionFactory.TryCreateTooltip(elementDescription, out var markdown);

            // Assert
            Assert.False(result);
            Assert.Null(markdown);
        }

        [Fact]
        public void TryCreateTooltip_Markup_Element_SingleAssociatedTagHelper_ReturnsTrue()
        {
            // Arrange
            var descriptionFactory = new DefaultLSPTagHelperTooltipFactory(LanguageServer);
            var associatedTagHelperInfos = new[]
            {
                new BoundElementDescriptionInfo("Microsoft.AspNetCore.SomeTagHelper", "<summary>Uses <see cref=\"T:System.Collections.List{System.String}\" />s</summary>"),
            };
            var elementDescription = new AggregateBoundElementDescription(associatedTagHelperInfos);
            // Act
            var result = descriptionFactory.TryCreateTooltip(elementDescription, out var markdown);

            // Assert
            Assert.True(result);
            Assert.Equal(@"**SomeTagHelper**

Uses `List<System.String>`s", markdown.Value);
            Assert.Equal(MarkupKind.Markdown, markdown.Kind);
        }

        [Fact]
        public void TryCreateTooltip_Markup_Element_PlainText_NoBold()
        {
            // Arrange
            var languageServer = LanguageServer;
            languageServer.ClientSettings.Capabilities.TextDocument.Completion.Value.CompletionItem.DocumentationFormat = new Container<MarkupKind>(MarkupKind.PlainText);
            var descriptionFactory = new DefaultLSPTagHelperTooltipFactory(languageServer);
            var associatedTagHelperInfos = new[]
            {
                new BoundElementDescriptionInfo("Microsoft.AspNetCore.SomeTagHelper", "<summary>Uses <see cref=\"T:System.Collections.List{System.String}\" />s</summary>"),
            };
            var elementDescription = new AggregateBoundElementDescription(associatedTagHelperInfos);

            // Act
            var result = descriptionFactory.TryCreateTooltip(elementDescription, out var markdown);

            // Assert
            Assert.True(result, "TryCreateTooltip should have succeeded");
            Assert.Equal(@"SomeTagHelper

Uses `List<System.String>`s", markdown.Value);
            Assert.Equal(MarkupKind.PlainText, markdown.Kind);
        }

        [Fact]
        public void TryCreateTooltip_Markup_Attribute_PlainText_NoBold()
        {
            // Arrange
            var languageServer = LanguageServer;
            languageServer.ClientSettings.Capabilities.TextDocument.Completion.Value.CompletionItem.DocumentationFormat = new Container<MarkupKind>(MarkupKind.PlainText);
            var descriptionFactory = new DefaultLSPTagHelperTooltipFactory(languageServer);
            var associatedAttributeDescriptions = new[]
            {
                new BoundAttributeDescriptionInfo(
                    returnTypeName: "System.String",
                    typeName: "Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName",
                    propertyName: "SomeProperty",
                    documentation: "<summary>Uses <see cref=\"T:System.Collections.List{System.String}\" />s</summary>")
            };
            var attributeDescription = new AggregateBoundAttributeDescription(associatedAttributeDescriptions);

            // Act
            var result = descriptionFactory.TryCreateTooltip(attributeDescription, out var markdown);

            // Assert
            Assert.True(result);
            Assert.Equal(@"string SomeTypeName.SomeProperty

Uses `List<System.String>`s", markdown.Value);
            Assert.Equal(MarkupKind.PlainText, markdown.Kind);
        }
        [Fact]
        public void TryCreateTooltip_Markup_Element_BothPlainTextAndMarkdown_IsBold()
        {
            // Arrange
            var languageServer = LanguageServer;
            languageServer.ClientSettings.Capabilities.TextDocument.Completion.Value.CompletionItem.DocumentationFormat = new Container<MarkupKind>(MarkupKind.PlainText, MarkupKind.Markdown);
            var descriptionFactory = new DefaultLSPTagHelperTooltipFactory(languageServer);
            var associatedTagHelperInfos = new[]
            {
                new BoundElementDescriptionInfo("Microsoft.AspNetCore.SomeTagHelper", "<summary>Uses <see cref=\"T:System.Collections.List{System.String}\" />s</summary>"),
            };
            var elementDescription = new AggregateBoundElementDescription(associatedTagHelperInfos);

            // Act
            var result = descriptionFactory.TryCreateTooltip(elementDescription, out var markdown);

            // Assert
            Assert.True(result);
            Assert.Equal(@"**SomeTagHelper**

Uses `List<System.String>`s", markdown.Value);
            Assert.Equal(MarkupKind.Markdown, markdown.Kind);
        }

        [Fact]
        public void TryCreateTooltip_Markup_Element_MultipleAssociatedTagHelpers_ReturnsTrue()
        {
            // Arrange
            var descriptionFactory = new DefaultLSPTagHelperTooltipFactory(LanguageServer);
            var associatedTagHelperInfos = new[]
            {
                new BoundElementDescriptionInfo("Microsoft.AspNetCore.SomeTagHelper", "<summary>\nUses <see cref=\"T:System.Collections.List{System.String}\" />s\n</summary>"),
                new BoundElementDescriptionInfo("Microsoft.AspNetCore.OtherTagHelper", "<summary>\nAlso uses <see cref=\"T:System.Collections.List{System.String}\" />s\n\r\n\r\r</summary>"),
            };
            var elementDescription = new AggregateBoundElementDescription(associatedTagHelperInfos);

            // Act
            var result = descriptionFactory.TryCreateTooltip(elementDescription, out var markdown);

            // Assert
            Assert.True(result);
            Assert.Equal(@"**SomeTagHelper**

Uses `List<System.String>`s
---
**OtherTagHelper**

Also uses `List<System.String>`s", markdown.Value);
            Assert.Equal(MarkupKind.Markdown, markdown.Kind);
        }

        [Fact]
        public void TryCreateTooltip_Markup_Attribute_SingleAssociatedAttribute_ReturnsTrue()
        {
            // Arrange
            var descriptionFactory = new DefaultLSPTagHelperTooltipFactory(LanguageServer);
            var associatedAttributeDescriptions = new[]
            {
                new BoundAttributeDescriptionInfo(
                    returnTypeName: "System.String",
                    typeName: "Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName",
                    propertyName: "SomeProperty",
                    documentation: "<summary>Uses <see cref=\"T:System.Collections.List{System.String}\" />s</summary>")
            };
            var attributeDescription = new AggregateBoundAttributeDescription(associatedAttributeDescriptions);

            // Act
            var result = descriptionFactory.TryCreateTooltip(attributeDescription, out var markdown);

            // Assert
            Assert.True(result);
            Assert.Equal(@"**string** SomeTypeName.**SomeProperty**

Uses `List<System.String>`s", markdown.Value);
            Assert.Equal(MarkupKind.Markdown, markdown.Kind);
        }

        [Fact]
        public void TryCreateTooltip_Markup_Attribute_MultipleAssociatedAttributes_ReturnsTrue()
        {
            // Arrange
            var descriptionFactory = new DefaultLSPTagHelperTooltipFactory(LanguageServer);
            var associatedAttributeDescriptions = new[]
            {
                new BoundAttributeDescriptionInfo(
                    returnTypeName: "System.String",
                    typeName: "Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName",
                    propertyName: "SomeProperty",
                    documentation: "<summary>Uses <see cref=\"T:System.Collections.List{System.String}\" />s</summary>"),
                new BoundAttributeDescriptionInfo(
                    propertyName: "AnotherProperty",
                    typeName: "Microsoft.AspNetCore.SomeTagHelpers.AnotherTypeName",
                    returnTypeName: "System.Boolean?",
                    documentation: "<summary>\nUses <see cref=\"T:System.Collections.List{System.String}\" />s\n</summary>"),
            };
            var attributeDescription = new AggregateBoundAttributeDescription(associatedAttributeDescriptions);

            // Act
            var result = descriptionFactory.TryCreateTooltip(attributeDescription, out var markdown);

            // Assert
            Assert.True(result);
            Assert.Equal(@"**string** SomeTypeName.**SomeProperty**

Uses `List<System.String>`s
---
**Boolean?** AnotherTypeName.**AnotherProperty**

Uses `List<System.String>`s", markdown.Value);
            Assert.Equal(MarkupKind.Markdown, markdown.Kind);
        }
    }
}

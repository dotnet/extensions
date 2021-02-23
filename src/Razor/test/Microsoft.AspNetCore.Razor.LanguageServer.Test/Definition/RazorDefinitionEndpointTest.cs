// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Definition
{
    public class RazorDefinitionEndpointTest : TagHelperServiceTestBase
    {
        private const string DefaultContent = @"@addTagHelper *, TestAssembly
<Component1 @test=""Increment""></Component1>
@code {
    public void Increment()
    {
    }
}";
        [Fact]
        public async Task GetOriginTagHelperBindingAsync_TagHelper_Element()
        {
            // Arrange
            var txt = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test1></test1>";
            var srcText = SourceText.From(txt);
            var codeDocument = CreateCodeDocument(txt, DefaultTagHelpers);
            var documentSnapshot = Mock.Of<DocumentSnapshot>(d => d.GetTextAsync() == Task.FromResult(srcText), MockBehavior.Strict);
            var position = new Position(1, 2);

            // Act
            var binding = await RazorDefinitionEndpoint.GetOriginTagHelperBindingAsync(documentSnapshot, codeDocument, position).ConfigureAwait(false);

            // Assert
            Assert.Equal("test1", binding.TagName);
            var descriptor = Assert.Single(binding.Descriptors);
            Assert.Equal("Test1TagHelper", descriptor.Name);
        }

        [Fact]
        public async Task GetOriginTagHelperBindingAsync_TagHelper_StartTag_WithAttribute()
        {
            // Arrange
            SetupDocument(out var codeDocument, out var documentSnapshot);
            var position = new Position(1, 2);

            // Act
            var binding = await RazorDefinitionEndpoint.GetOriginTagHelperBindingAsync(documentSnapshot, codeDocument, position).ConfigureAwait(false);

            // Assert
            Assert.Equal("Component1", binding.TagName);
            Assert.Collection(binding.Descriptors,
                d => Assert.Equal("Component1TagHelper", d.Name),
                d => Assert.Equal("TestDirectiveAttribute", d.Name));
        }

        [Fact]
        public async Task GetOriginTagHelperBindingAsync_TagHelper_EndTag_WithAttribute()
        {
            // Arrange
            SetupDocument(out var codeDocument, out var documentSnapshot);
            var position = new Position(1, 35);

            // Act
            var binding = await RazorDefinitionEndpoint.GetOriginTagHelperBindingAsync(documentSnapshot, codeDocument, position).ConfigureAwait(false);

            // Assert
            Assert.Equal("Component1", binding.TagName);
            Assert.Collection(binding.Descriptors,
                d => Assert.Equal("Component1TagHelper", d.Name),
                d => Assert.Equal("TestDirectiveAttribute", d.Name));
        }

        [Fact]
        public async Task GetOriginTagHelperBindingAsync_TagHelper_Attribute_ReturnsNull()
        {
            // Arrange
            SetupDocument(out var codeDocument, out var documentSnapshot);
            var position = new Position(1, 14);

            // Act
            var binding = await RazorDefinitionEndpoint.GetOriginTagHelperBindingAsync(documentSnapshot, codeDocument, position).ConfigureAwait(false);

            // Assert
            Assert.Null(binding);
        }

        [Fact]
        public async Task GetOriginTagHelperBindingAsync_TagHelper_AttributeValue_ReturnsNull()
        {
            // Arrange
            SetupDocument(out var codeDocument, out var documentSnapshot);
            var position = new Position(1, 24);

            // Act
            var binding = await RazorDefinitionEndpoint.GetOriginTagHelperBindingAsync(documentSnapshot, codeDocument, position).ConfigureAwait(false);

            // Assert
            Assert.Null(binding);
        }

        [Fact]
        public async Task GetOriginTagHelperBindingAsync_TagHelper_AfterAttributeEquals_ReturnsNull()
        {
            // Arrange
            SetupDocument(out var codeDocument, out var documentSnapshot);
            var position = new Position(1, 18);

            // Act
            var binding = await RazorDefinitionEndpoint.GetOriginTagHelperBindingAsync(documentSnapshot, codeDocument, position).ConfigureAwait(false);

            // Assert
            Assert.Null(binding);
        }

        [Fact]
        public async Task GetOriginTagHelperBindingAsync_TagHelper_AttributeEnd_ReturnsNull()
        {
            // Arrange
            SetupDocument(out var codeDocument, out var documentSnapshot);
            var position = new Position(1, 29);

            // Act
            var binding = await RazorDefinitionEndpoint.GetOriginTagHelperBindingAsync(documentSnapshot, codeDocument, position).ConfigureAwait(false);

            // Assert
            Assert.Null(binding);
        }

        [Fact]
        public async Task GetOriginTagHelperBindingAsync_TagHelper_MultipleAttributes()
        {
            // Arrange
            var content = @"@addTagHelper *, TestAssembly
<Component1 @test=""Increment"" @minimized></Component1>
@code {
    public void Increment()
    {
    }
}";
            SetupDocument(out var codeDocument, out var documentSnapshot, content);
            var position = new Position(1, 2);

            // Act
            var binding = await RazorDefinitionEndpoint.GetOriginTagHelperBindingAsync(documentSnapshot, codeDocument, position).ConfigureAwait(false);

            // Assert
            Assert.Equal("Component1", binding.TagName);
            Assert.Collection(binding.Descriptors,
                d => Assert.Equal("Component1TagHelper", d.Name),
                d => Assert.Equal("TestDirectiveAttribute", d.Name),
                d => Assert.Equal("MinimizedDirectiveAttribute", d.Name));
        }

        [Fact]
        public async Task GetOriginTagHelperBindingAsync_TagHelper_MalformedElement()
        {
            // Arrange
            var content = @"@addTagHelper *, TestAssembly
<Component1</Component1>
@code {
    public void Increment()
    {
    }
}";
            SetupDocument(out var codeDocument, out var documentSnapshot, content);
            var position = new Position(1, 2);

            // Act
            var binding = await RazorDefinitionEndpoint.GetOriginTagHelperBindingAsync(documentSnapshot, codeDocument, position).ConfigureAwait(false);

            // Assert
            Assert.Equal("Component1", binding.TagName);
            var descriptor = Assert.Single(binding.Descriptors);
            Assert.Equal("Component1TagHelper", descriptor.Name);
        }

        [Fact]
        public async Task GetOriginTagHelperBindingAsync_TagHelper_MalformedAttribute()
        {

            // Arrange
            var content = @"@addTagHelper *, TestAssembly
<Component1 @test=""Increment></Component1>
@code {
    public void Increment()
    {
    }
}";
            SetupDocument(out var codeDocument, out var documentSnapshot, content);
            var position = new Position(1, 2);

            // Act
            var binding = await RazorDefinitionEndpoint.GetOriginTagHelperBindingAsync(documentSnapshot, codeDocument, position).ConfigureAwait(false);

            // Assert
            Assert.Equal("Component1", binding.TagName);
            Assert.Collection(binding.Descriptors,
                d => Assert.Equal("Component1TagHelper", d.Name),
                d => Assert.Equal("TestDirectiveAttribute", d.Name));
        }

        [Fact]
        public async Task GetOriginTagHelperBindingAsync_HTML_MarkupElement()
        {
            // Arrange
            var content = $"@addTagHelper *, TestAssembly{Environment.NewLine}<p><strong></strong></p>";
            SetupDocument(out var codeDocument, out var documentSnapshot, content);
            var position = new Position(1, 6);

            // Act
            var binding = await RazorDefinitionEndpoint.GetOriginTagHelperBindingAsync(documentSnapshot, codeDocument, position).ConfigureAwait(false);

            // Assert
            Assert.Null(binding);
        }

        private void SetupDocument(out Language.RazorCodeDocument codeDocument, out DocumentSnapshot documentSnapshot, string content = DefaultContent)
        {
            var sourceText = SourceText.From(content);
            codeDocument = CreateCodeDocument(content, "text.razor", DefaultTagHelpers);
            documentSnapshot = Mock.Of<DocumentSnapshot>(d => d.GetTextAsync() == Task.FromResult(sourceText), MockBehavior.Strict);
        }
    }
}

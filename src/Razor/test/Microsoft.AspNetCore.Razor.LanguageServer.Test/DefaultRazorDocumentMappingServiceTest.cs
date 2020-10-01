// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class DefaultRazorDocumentMappingServiceTest
    {
        [Fact]
        public void TryMapToProjectedDocumentPosition_NotMatchingAnyMapping()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                "test razor source",
                "test C# source",
                new[] { new SourceMapping(new SourceSpan(2, 100), new SourceSpan(0, 100)) });

            // Act
            var result = service.TryMapToProjectedDocumentPosition(
                codeDoc,
                1,
                out var projectedPosition,
                out var projectedPositionIndex);

            // Assert
            Assert.False(result);
            Assert.Equal(default, projectedPosition);
            Assert.Equal(default, projectedPositionIndex);
        }

        [Fact]
        public void TryMapToProjectedDocumentPosition_CSharp_OnLeadingEdge()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                "Line 1\nLine 2 @{ var abc;\nvar def; }",
                "\n// Prefix\n var abc;\nvar def; \n// Suffix",
                new[] {
                    new SourceMapping(new SourceSpan(0, 1), new SourceSpan(0, 1)),
                    new SourceMapping(new SourceSpan(16, 19), new SourceSpan(11, 19))
                });

            // Act
            var result = service.TryMapToProjectedDocumentPosition(
                codeDoc,
                16,
                out var projectedPosition,
                out var projectedPositionIndex);

            // Assert
            Assert.True(result);
            Assert.Equal(2, projectedPosition.Line);
            Assert.Equal(0, projectedPosition.Character);
            Assert.Equal(11, projectedPositionIndex);
        }

        [Fact]
        public void TryMapToProjectedDocumentPosition_CSharp_InMiddle()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                "Line 1\nLine 2 @{ var abc;\nvar def; }",
                "\n// Prefix\n var abc;\nvar def; \n// Suffix",
                new[] {
                    new SourceMapping(new SourceSpan(0, 1), new SourceSpan(0, 1)),
                    new SourceMapping(new SourceSpan(16, 19), new SourceSpan(11, 19))
                });

            // Act
            var result = service.TryMapToProjectedDocumentPosition(
                codeDoc,
                28,
                out var projectedPosition,
                out var projectedPositionIndex);

            // Assert
            Assert.True(result);
            Assert.Equal(3, projectedPosition.Line);
            Assert.Equal(2, projectedPosition.Character);
            Assert.Equal(23, projectedPositionIndex);
        }

        [Fact]
        public void TryMapToProjectedDocumentPosition_CSharp_OnTrailingEdge()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                "Line 1\nLine 2 @{ var abc;\nvar def; }",
                "\n// Prefix\n var abc;\nvar def; \n// Suffix",
                new[] {
                    new SourceMapping(new SourceSpan(0, 1), new SourceSpan(0, 1)),
                    new SourceMapping(new SourceSpan(16, 19), new SourceSpan(11, 19))
                });

            // Act
            var result = service.TryMapToProjectedDocumentPosition(
                codeDoc,
                35,
                out var projectedPosition,
                out var projectedPositionIndex);

            // Assert
            Assert.True(result);
            Assert.Equal(3, projectedPosition.Line);
            Assert.Equal(9, projectedPosition.Character);
            Assert.Equal(30, projectedPositionIndex);
        }

        [Fact]
        public void TryMapFromProjectedDocumentPosition_NotMatchingAnyMapping()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                razorSource: "test razor source",
                projectedCSharpSource: "projectedCSharpSource: test C# source",
                new[] { new SourceMapping(new SourceSpan(2, 100), new SourceSpan(2, 100)) });

            // Act
            var result = service.TryMapFromProjectedDocumentPosition(
                codeDoc,
                1,
                out var hostDocumentPosition,
                out var hostDocumentIndex);

            // Assert
            Assert.False(result);
            Assert.Equal(default, hostDocumentPosition);
            Assert.Equal(default, hostDocumentIndex);
        }

        [Fact]
        public void TryMapFromProjectedDocumentPosition_CSharp_OnLeadingEdge()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                razorSource: "Line 1\nLine 2 @{ var abc;\nvar def; }",
                projectedCSharpSource: "\n// Prefix\n var abc;\nvar def; \n// Suffix",
                new[] {
                    new SourceMapping(new SourceSpan(0, 1), new SourceSpan(0, 1)),
                    new SourceMapping(new SourceSpan(16, 19), new SourceSpan(11, 19))
                });

            // Act
            var result = service.TryMapFromProjectedDocumentPosition(
                codeDoc,
                11, // @{|
                out var hostDocumentPosition,
                out var hostDocumentIndex);

            // Assert
            Assert.True(result);
            Assert.Equal(1, hostDocumentPosition.Line);
            Assert.Equal(9, hostDocumentPosition.Character);
            Assert.Equal(16, hostDocumentIndex);
        }

        [Fact]
        public void TryMapFromProjectedDocumentPosition_CSharp_InMiddle()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                razorSource: "Line 1\nLine 2 @{ var abc;\nvar def; }",
                projectedCSharpSource: "\n// Prefix\n var abc;\nvar def; \n// Suffix",
                new[] {
                    new SourceMapping(new SourceSpan(0, 1), new SourceSpan(0, 1)),
                    new SourceMapping(new SourceSpan(16, 19), new SourceSpan(11, 19))
                });

            // Act
            var result = service.TryMapFromProjectedDocumentPosition(
                codeDoc,
                21, // |var def
                out var hostDocumentPosition,
                out var hostDocumentIndex);

            // Assert
            Assert.True(result);
            Assert.Equal(2, hostDocumentPosition.Line);
            Assert.Equal(0, hostDocumentPosition.Character);
            Assert.Equal(26, hostDocumentIndex);
        }

        [Fact]
        public void TryMapFromProjectedDocumentPosition_CSharp_OnTrailingEdge()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                razorSource: "Line 1\nLine 2 @{ var abc;\nvar def; }",
                projectedCSharpSource: "\n// Prefix\n var abc;\nvar def; \n// Suffix",
                new[] {
                    new SourceMapping(new SourceSpan(0, 1), new SourceSpan(0, 1)),
                    new SourceMapping(new SourceSpan(16, 19), new SourceSpan(11, 19))
                });

            // Act
            var result = service.TryMapFromProjectedDocumentPosition(
                codeDoc,
                30, // def; |}
                out var hostDocumentPosition,
                out var hostDocumentIndex);

            // Assert
            Assert.True(result);
            Assert.Equal(2, hostDocumentPosition.Line);
            Assert.Equal(9, hostDocumentPosition.Character);
            Assert.Equal(35, hostDocumentIndex);
        }

        [Fact]
        public void TryMapToProjectedDocumentRange_CSharp()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                razorSource: "Line 1\nLine 2 @{ var abc;\nvar def; }",
                projectedCSharpSource: "\n// Prefix\n var abc;\nvar def; \n// Suffix",
                new[] {
                    new SourceMapping(new SourceSpan(0, 1), new SourceSpan(0, 1)),
                    new SourceMapping(new SourceSpan(16, 19), new SourceSpan(11, 19))
                });
            var range = new Range(new Position(1, 10), new Position(1, 13));

            // Act
            var result = service.TryMapToProjectedDocumentRange(
                codeDoc,
                range, // |var| abc
                out var projectedRange);

            // Assert
            Assert.True(result);
            Assert.Equal(2, projectedRange.Start.Line);
            Assert.Equal(1, projectedRange.Start.Character);
            Assert.Equal(2, projectedRange.End.Line);
            Assert.Equal(4, projectedRange.End.Character);
        }

        [Fact]
        public void TryMapToProjectedDocumentRange_CSharp_MissingSourceMappings()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                razorSource: "Line 1\nLine 2 @{ var abc;\nvar def; }",
                projectedCSharpSource: "\n// Prefix\n var abc;\nvar def; \n// Suffix",
                new[] {
                    new SourceMapping(new SourceSpan(0, 1), new SourceSpan(0, 1)),
                });
            var range = new Range(new Position(1, 10), new Position(1, 13));

            // Act
            var result = service.TryMapToProjectedDocumentRange(
                codeDoc,
                range, // |var| abc
                out var projectedRange);

            // Assert
            Assert.False(result);
            Assert.Equal(default, projectedRange);
        }

        [Fact]
        public void TryMapToProjectedDocumentRange_CSharp_End_LessThan_Start()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                razorSource: "Line 1\nLine 2 @{ var abc;\nvar def; }",
                projectedCSharpSource: "\n// Prefix\n var abc;\nvar def; \n// Suffix",
                new[] {
                    new SourceMapping(new SourceSpan(0, 1), new SourceSpan(0, 1)),
                    new SourceMapping(new SourceSpan(16, 3), new SourceSpan(11, 3)),
                    new SourceMapping(new SourceSpan(19, 10), new SourceSpan(5, 10))
                });
            var range = new Range(new Position(1, 10), new Position(1, 13));

            // Act
            var result = service.TryMapToProjectedDocumentRange(
                codeDoc,
                range, // |var| abc
                out var projectedRange);

            // Assert
            Assert.False(result);
            Assert.Equal(default, projectedRange);
        }

        [Fact]
        public void GetLanguageKindCore_TagHelperElementOwnsName()
        {
            // Arrange
            var descriptor = TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly");
            descriptor.TagMatchingRule(rule => rule.TagName = "test");
            descriptor.SetTypeName("TestTagHelper");
            var text = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test>@Name</test>";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text, new[] { descriptor.Build() });

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 32 + Environment.NewLine.Length);

            // Assert
            Assert.Equal(RazorLanguageKind.Html, languageKind);
        }

        [Fact]
        public void GetLanguageKindCore_TagHelpersDoNotOwnTrailingEdge()
        {
            // Arrange
            var descriptor = TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly");
            descriptor.TagMatchingRule(rule => rule.TagName = "test");
            descriptor.SetTypeName("TestTagHelper");
            var text = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test></test>";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text, new[] { descriptor.Build() });

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 42 + Environment.NewLine.Length);

            // Assert
            Assert.Equal(RazorLanguageKind.Razor, languageKind);
        }

        [Fact]
        public void GetLanguageKindCore_TagHelperNestedCSharpAttribute()
        {
            // Arrange
            var descriptor = TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly");
            descriptor.TagMatchingRule(rule => rule.TagName = "test");
            descriptor.BindAttribute(builder =>
            {
                builder.Name = "asp-int";
                builder.TypeName = typeof(int).FullName;
                builder.SetPropertyName("AspInt");
            });
            descriptor.SetTypeName("TestTagHelper");
            var text = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test asp-int='123'></test>";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text, new[] { descriptor.Build() });

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 46 + Environment.NewLine.Length);

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, languageKind);
        }

        [Fact]
        public void GetLanguageKindCore_CSharp()
        {
            // Arrange
            var text = "<p>@Name</p>";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 5);

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, languageKind);
        }

        [Fact]
        public void GetLanguageKindCore_Html()
        {
            // Arrange
            var text = "<p>Hello World</p>";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 5);

            // Assert
            Assert.Equal(RazorLanguageKind.Html, languageKind);
        }

        [Fact]
        public void GetLanguageKindCore_DefaultsToRazorLanguageIfCannotLocateOwner()
        {
            // Arrange
            var text = "<p>Hello World</p>";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, text.Length + 1);

            // Assert
            Assert.Equal(RazorLanguageKind.Razor, languageKind);
        }

        [Fact]
        public void GetLanguageKindCore_HtmlEdgeEnd()
        {
            // Arrange
            var text = "Hello World";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, text.Length);

            // Assert
            Assert.Equal(RazorLanguageKind.Html, languageKind);
        }

        [Fact]
        public void GetLanguageKindCore_CSharpEdgeEnd()
        {
            // Arrange
            var text = "@Name";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, text.Length);

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, languageKind);
        }

        [Fact]
        public void GetLanguageKindCore_RazorEdgeWithCSharp()
        {
            // Arrange
            var text = "@{}";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 2);

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, languageKind);
        }

        [Fact]
        public void GetLanguageKindCore_CSharpEdgeWithCSharpMarker()
        {
            // Arrange
            var text = "@{var x = 1;}";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 12);

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, languageKind);
        }

        [Fact]
        public void GetLanguageKindCore_ExplicitExpressionStartCSharp()
        {
            // Arrange
            var text = "@()";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 2);

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, languageKind);
        }

        [Fact]
        public void GetLanguageKindCore_ExplicitExpressionInProgressCSharp()
        {
            // Arrange
            var text = "@(Da)";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 4);

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, languageKind);
        }

        [Fact]
        public void GetLanguageKindCore_ImplicitExpressionStartCSharp()
        {
            // Arrange
            var text = "@";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 1);

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, languageKind);
        }

        [Fact]
        public void GetLanguageKindCore_ImplicitExpressionInProgressCSharp()
        {
            // Arrange
            var text = "@Da";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 3);

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, languageKind);
        }

        [Fact]
        public void GetLanguageKindCore_RazorEdgeWithHtml()
        {
            // Arrange
            var text = "@{<br />}";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 2);

            // Assert
            Assert.Equal(RazorLanguageKind.Html, languageKind);
        }

        private (IReadOnlyList<ClassifiedSpanInternal> classifiedSpans, IReadOnlyList<TagHelperSpanInternal> tagHelperSpans) GetClassifiedSpans(string text, IReadOnlyList<TagHelperDescriptor> tagHelpers = null)
        {
            var codeDocument = CreateCodeDocument(text, tagHelpers);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var classifiedSpans = syntaxTree.GetClassifiedSpans();
            var tagHelperSpans = syntaxTree.GetTagHelperSpans();
            return (classifiedSpans, tagHelperSpans);
        }

        private static RazorCodeDocument CreateCodeDocument(string text, IReadOnlyList<TagHelperDescriptor> tagHelpers = null)
        {
            tagHelpers = tagHelpers ?? Array.Empty<TagHelperDescriptor>();
            var sourceDocument = TestRazorSourceDocument.Create(text);
            var projectEngine = RazorProjectEngine.Create(builder => { });
            var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, "mvc", Array.Empty<RazorSourceDocument>(), tagHelpers);
            return codeDocument;
        }

        private static RazorCodeDocument CreateCodeDocumentWithCSharpProjection(string razorSource, string projectedCSharpSource, IEnumerable<SourceMapping> sourceMappings)
        {
            var codeDocument = CreateCodeDocument(razorSource, Array.Empty<TagHelperDescriptor>());
            var csharpDocument = RazorCSharpDocument.Create(
                    projectedCSharpSource,
                    RazorCodeGenerationOptions.CreateDefault(),
                    Enumerable.Empty<RazorDiagnostic>(),
                    sourceMappings,
                    Enumerable.Empty<LinePragma>());
            codeDocument.SetCSharpDocument(csharpDocument);
            return codeDocument;
        }
    }
}

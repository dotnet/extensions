// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Xunit;

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

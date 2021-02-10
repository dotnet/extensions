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
        public void TryMapFromProjectedDocumentRange_Strict_StartOnlyMaps_ReturnsFalse()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "__o = DateTime.Now;",
                new[] { new SourceMapping(new SourceSpan(4, 12), new SourceSpan(6, 12)) });
            var projectedRange = new Range()
            {
                Start = new Position(0, 10),
                End = new Position(0, 19),
            };

            // Act
            var result = service.TryMapFromProjectedDocumentRange(
                codeDoc,
                projectedRange,
                MappingBehavior.Strict,
                out var originalRange);

            // Assert
            Assert.False(result);
            Assert.Equal(default, originalRange);
        }

        [Fact]
        public void TryMapFromProjectedDocumentRange_Strict_EndOnlyMaps_ReturnsFalse()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "__o = DateTime.Now;",
                new[] { new SourceMapping(new SourceSpan(4, 12), new SourceSpan(6, 12)) });
            var projectedRange = new Range()
            {
                Start = new Position(0, 0),
                End = new Position(0, 12),
            };

            // Act
            var result = service.TryMapFromProjectedDocumentRange(
                codeDoc,
                projectedRange,
                MappingBehavior.Strict,
                out var originalRange);

            // Assert
            Assert.False(result);
            Assert.Equal(default, originalRange);
        }

        [Fact]
        public void TryMapFromProjectedDocumentRange_Strict_StartAndEndMap_ReturnsTrue()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "__o = DateTime.Now;",
                new[] { new SourceMapping(new SourceSpan(4, 12), new SourceSpan(6, 12)) });
            var projectedRange = new Range()
            {
                Start = new Position(0, 6),
                End = new Position(0, 18),
            };
            var expectedOriginalRange = new Range()
            {
                Start = new Position(0, 4),
                End = new Position(0, 16)
            };

            // Act
            var result = service.TryMapFromProjectedDocumentRange(
                codeDoc,
                projectedRange,
                MappingBehavior.Strict,
                out var originalRange);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedOriginalRange, originalRange);
        }

        [Fact]
        public void TryMapFromProjectedDocumentRange_Inclusive_DirectlyMaps_ReturnsTrue()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "__o = DateTime.Now;",
                new[] { new SourceMapping(new SourceSpan(4, 12), new SourceSpan(6, 12)) });
            var projectedRange = new Range()
            {
                Start = new Position(0, 6),
                End = new Position(0, 18),
            };
            var expectedOriginalRange = new Range()
            {
                Start = new Position(0, 4),
                End = new Position(0, 16)
            };

            // Act
            var result = service.TryMapFromProjectedDocumentRange(
                codeDoc,
                projectedRange,
                MappingBehavior.Inclusive,
                out var originalRange);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedOriginalRange, originalRange);
        }

        [Fact]
        public void TryMapFromProjectedDocumentRange_Inclusive_StartSinglyIntersects_ReturnsTrue()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "__o = DateTime.Now;",
                new[] { new SourceMapping(new SourceSpan(4, 12), new SourceSpan(6, 12)) });
            var projectedRange = new Range()
            {
                Start = new Position(0, 10),
                End = new Position(0, 19),
            };
            var expectedOriginalRange = new Range()
            {
                Start = new Position(0, 4),
                End = new Position(0, 16)
            };

            // Act
            var result = service.TryMapFromProjectedDocumentRange(
                codeDoc,
                projectedRange,
                MappingBehavior.Inclusive,
                out var originalRange);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedOriginalRange, originalRange);
        }

        [Fact]
        public void TryMapFromProjectedDocumentRange_Inclusive_EndSinglyIntersects_ReturnsTrue()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "__o = DateTime.Now;",
                new[] { new SourceMapping(new SourceSpan(4, 12), new SourceSpan(6, 12)) });
            var projectedRange = new Range()
            {
                Start = new Position(0, 0),
                End = new Position(0, 10),
            };
            var expectedOriginalRange = new Range()
            {
                Start = new Position(0, 4),
                End = new Position(0, 16)
            };

            // Act
            var result = service.TryMapFromProjectedDocumentRange(
                codeDoc,
                projectedRange,
                MappingBehavior.Inclusive,
                out var originalRange);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedOriginalRange, originalRange);
        }

        [Fact]
        public void TryMapFromProjectedDocumentRange_Inclusive_StartDoublyIntersects_ReturnsFalse()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "__o = DateTime.Now;",
                new[]
                {
                    new SourceMapping(new SourceSpan(4, 8), new SourceSpan(6, 8)), // DateTime
                    new SourceMapping(new SourceSpan(12, 4), new SourceSpan(14, 4)) // .Now
                });
            var projectedRange = new Range()
            {
                Start = new Position(0, 14),
                End = new Position(0, 19),
            };

            // Act
            var result = service.TryMapFromProjectedDocumentRange(
                codeDoc,
                projectedRange,
                MappingBehavior.Inclusive,
                out var originalRange);

            // Assert
            Assert.False(result);
            Assert.Equal(default, originalRange);
        }

        [Fact]
        public void TryMapFromProjectedDocumentRange_Inclusive_EndDoublyIntersects_ReturnsFalse()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "__o = DateTime.Now;",
                new[]
                {
                    new SourceMapping(new SourceSpan(4, 8), new SourceSpan(6, 8)), // DateTime
                    new SourceMapping(new SourceSpan(12, 4), new SourceSpan(14, 4)) // .Now
                });
            var projectedRange = new Range()
            {
                Start = new Position(0, 0),
                End = new Position(0, 14),
            };

            // Act
            var result = service.TryMapFromProjectedDocumentRange(
                codeDoc,
                projectedRange,
                MappingBehavior.Inclusive,
                out var originalRange);

            // Assert
            Assert.False(result);
            Assert.Equal(default, originalRange);
        }

        [Fact]
        public void TryMapFromProjectedDocumentRange_Inclusive_OverlapsSingleMapping_ReturnsTrue()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "__o = DateTime.Now;",
                new[] { new SourceMapping(new SourceSpan(4, 12), new SourceSpan(6, 12)) });
            var projectedRange = new Range()
            {
                Start = new Position(0, 0),
                End = new Position(0, 19),
            };
            var expectedOriginalRange = new Range()
            {
                Start = new Position(0, 4),
                End = new Position(0, 16)
            };

            // Act
            var result = service.TryMapFromProjectedDocumentRange(
                codeDoc,
                projectedRange,
                MappingBehavior.Inclusive,
                out var originalRange);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedOriginalRange, originalRange);
        }

        [Fact]
        public void TryMapFromProjectedDocumentRange_Inclusive_OverlapsTwoMappings_ReturnsFalse()
        {
            // Arrange
            var service = new DefaultRazorDocumentMappingService();
            var codeDoc = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "__o = DateTime.Now;",
                new[]
                {
                    new SourceMapping(new SourceSpan(4, 8), new SourceSpan(6, 8)), // DateTime
                    new SourceMapping(new SourceSpan(12, 4), new SourceSpan(14, 4)) // .Now
                });
            var projectedRange = new Range()
            {
                Start = new Position(0, 0),
                End = new Position(0, 19),
            };

            // Act
            var result = service.TryMapFromProjectedDocumentRange(
                codeDoc,
                projectedRange,
                MappingBehavior.Inclusive,
                out var originalRange);

            // Assert
            Assert.False(result);
            Assert.Equal(default, originalRange);
        }

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
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 32 + Environment.NewLine.Length, text.Length);

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
            var text = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test></test>@DateTime.Now";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text, new[] { descriptor.Build() });

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 42 + Environment.NewLine.Length, text.Length);

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
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 46 + Environment.NewLine.Length, text.Length);

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
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 5, text.Length);

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
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 5, text.Length);

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
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, text.Length + 1, text.Length);

            // Assert
            Assert.Equal(RazorLanguageKind.Razor, languageKind);
        }

        [Fact]
        public void GetLanguageKindCore_GetsLastClassifiedSpanLanguageIfAtEndOfDocument()
        {
            // Arrange
            var text = $"<strong>Something</strong>{Environment.NewLine}<App>";
            var classifiedSpans = new List<ClassifiedSpanInternal>()
            {
               new ClassifiedSpanInternal(
                   new SourceSpan(0, 0),
                   blockSpan: new SourceSpan(absoluteIndex: 0, lineIndex: 0, characterIndex: 0, length: text.Length),
                   SpanKindInternal.Transition,
                   blockKind: default,
                   acceptedCharacters: default),
               new ClassifiedSpanInternal(
                   new SourceSpan(0, 26),
                   blockSpan: default,
                   SpanKindInternal.Markup,
                   blockKind: default,
                   acceptedCharacters: default)
            };
            var tagHelperSpans = Array.Empty<TagHelperSpanInternal>();

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, text.Length, text.Length);

            // Assert
            Assert.Equal(RazorLanguageKind.Html, languageKind);
        }

        [Fact]
        public void GetLanguageKindCore_HtmlEdgeEnd()
        {
            // Arrange
            var text = "Hello World";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, text.Length, text.Length);

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
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, text.Length, text.Length);

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
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 2, text.Length);

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
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 12, text.Length);

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
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 2, text.Length);

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
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 4, text.Length);

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
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 1, text.Length);

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
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 3, text.Length);

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
            var languageKind = DefaultRazorDocumentMappingService.GetLanguageKindCore(classifiedSpans, tagHelperSpans, 2, text.Length);

            // Assert
            Assert.Equal(RazorLanguageKind.Html, languageKind);
        }

        private static (IReadOnlyList<ClassifiedSpanInternal> classifiedSpans, IReadOnlyList<TagHelperSpanInternal> tagHelperSpans) GetClassifiedSpans(string text, IReadOnlyList<TagHelperDescriptor> tagHelpers = null)
        {
            var codeDocument = CreateCodeDocument(text, tagHelpers);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var classifiedSpans = syntaxTree.GetClassifiedSpans();
            var tagHelperSpans = syntaxTree.GetTagHelperSpans();
            return (classifiedSpans, tagHelperSpans);
        }

        private static RazorCodeDocument CreateCodeDocument(string text, IReadOnlyList<TagHelperDescriptor> tagHelpers = null)
        {
            tagHelpers ??= Array.Empty<TagHelperDescriptor>();
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
